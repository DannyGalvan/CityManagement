# ./airflow/dags/kafka_smoke_test.py
from __future__ import annotations
from datetime import datetime, timedelta
import json, time, os

from airflow import DAG
from airflow.operators.python import PythonOperator

from confluent_kafka import Producer, Consumer, KafkaException, KafkaError
from confluent_kafka.admin import AdminClient, NewTopic

BOOTSTRAP = os.getenv("KAFKA_BOOTSTRAP", "kafka:9092")
TOPIC      = os.getenv("KAFKA_TOPIC_SMOKE", "city-smoke")
GROUP_ID   = os.getenv("KAFKA_GROUP_SMOKE", "airflow-smoke")
N_MESSAGES = int(os.getenv("SMOKE_N", "5"))
TIMEOUT_S  = int(os.getenv("SMOKE_TIMEOUT", "20"))

def ensure_topic():
    admin = AdminClient({"bootstrap.servers": BOOTSTRAP})
    md = admin.list_topics(timeout=10)
    if TOPIC in [t for t in md.topics]:
        return
    futures = admin.create_topics([NewTopic(TOPIC, num_partitions=1, replication_factor=1)])
    for _, f in futures.items():
        try:
            f.result()
        except Exception as e:
            # Si otro proceso lo creó en paralelo, ignora el error "TopicAlreadyExists"
            if "Topic already exists" not in str(e):
                raise

def produce_messages():
    p = Producer({"bootstrap.servers": BOOTSTRAP})
    for i in range(N_MESSAGES):
        evt = {"kind":"smoke", "i": i, "ts": datetime.utcnow().isoformat()}
        p.produce(TOPIC, json.dumps(evt).encode("utf-8"))
    p.flush(10)

def consume_and_assert(**context):
    import re, time
    # group.id único por corrida → garantiza lectura desde earliest
    run_suffix = context.get("run_id") or str(int(time.time()))
    run_suffix = re.sub(r"[^a-zA-Z0-9._-]+", "_", run_suffix)  # sanitiza
    group_id = f"{GROUP_ID}-{run_suffix}"

    c = Consumer({
        "bootstrap.servers": BOOTSTRAP,
        "group.id": group_id,
        "auto.offset.reset": "earliest",   # aplica al ser grupo nuevo
        "enable.auto.commit": False,
        "session.timeout.ms": 10000,
        "max.poll.interval.ms": 300000,
    })

    c.subscribe([TOPIC])

    got = 0
    deadline = time.time() + max(TIMEOUT_S, 30)  # espera al menos 30s
    while time.time() < deadline and got < N_MESSAGES:
        msg = c.poll(1.0)
        if msg is None:
            continue
        if msg.error():
            if msg.error().code() != KafkaError._PARTITION_EOF:
                raise KafkaException(msg.error())
            continue
        got += 1

    c.close()
    if got < N_MESSAGES:
        raise RuntimeError(f"Smoke test: esperados {N_MESSAGES}, recibidos {got}")

default_args = {
    "owner": "data-platform",
    "retries": 2,
    "retry_delay": timedelta(seconds=30)
}

with DAG(
    dag_id="kafka_smoke_test",
    default_args=default_args,
    start_date=datetime(2025, 10, 1),
    schedule_interval="*/15 * * * *",  # cada 15 min (ajústalo)
    catchup=False,
    max_active_runs=1,
    tags=["kafka","smoke","monitoring"]
) as dag:

    t1 = PythonOperator(task_id="ensure_topic", python_callable=ensure_topic)
    t2 = PythonOperator(task_id="produce", python_callable=produce_messages)
    t3 = PythonOperator(task_id="consume_and_assert", python_callable=consume_and_assert)

    t1 >> t2 >> t3
