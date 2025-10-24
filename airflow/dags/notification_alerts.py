from __future__ import annotations

import os
import json
from datetime import datetime

from airflow import DAG
from airflow.decorators import task
from airflow.providers.postgres.hooks.postgres import PostgresHook
from airflow.utils.dates import days_ago

# POSTGRES CONN ID
POSTGRES_CONN_ID = "alerts_postgres"  # Cambia esto si usas otro Conn Id

with DAG(
    dag_id="notify_alerts_dag",
    description="Lee las alertas más recientes en Postgres y genera un archivo log único por ejecución",
    start_date=days_ago(1),  # Comienza desde ayer, ajustable según tus necesidades
    schedule_interval="*/5 * * * *",  # Ejecuta cada 5 minutos
    catchup=False,
    tags=["alerts", "postgres", "log"],
) as dag:

    @task(task_id="extract_alerts_from_postgres")
    def extract_alerts(last_execution_date: datetime):
        """
        Se conecta a Postgres y devuelve una lista de alertas creadas después de la última ejecución.
        """
        hook = PostgresHook(postgres_conn_id=POSTGRES_CONN_ID)

        # Ajusta la consulta para solo traer alertas después de la última ejecución
        last_execution_date_obj = datetime.fromisoformat(last_execution_date)

        sql = f"""
        SELECT
            alert_id,
            correlation_id,
            type,
            score,
            zone,
            window_start,
            window_end,
            evidence,
            created_at
        FROM public.alerts
        WHERE created_at > '{last_execution_date_obj.isoformat()}'
        ORDER BY created_at DESC;
        """

        records = hook.get_records(sql)

        lines = []
        header = "timestamp\talert_id\tcorrelation_id\ttype\tscore\tzone\twindow_start\twindow_end\tevidence_json"
        lines.append(header)

        for (alert_id, correlation_id, type_, score, zone, window_start, window_end, evidence, created_at) in records:
            # Formatea la salida
            created_at_str = created_at.isoformat() if hasattr(created_at, "isoformat") else str(created_at)

            # Compacta el JSON (si viene como dict), o usa el string plano
            if isinstance(evidence, (dict, list)):
                evidence_str = json.dumps(evidence, ensure_ascii=False, separators=(",", ":"))
            else:
                # Si viene como texto, intenta parsear; si no, deja tal cual
                try:
                    evidence_str = json.dumps(json.loads(str(evidence)), ensure_ascii=False, separators=(",", ":"))
                except Exception:
                    evidence_str = str(evidence).replace("\n", " ").replace("\t", " ")

            line = f"{created_at_str}\t{alert_id}\t{correlation_id}\t{type_}\t{score}\t{zone}\t{window_start}\t{window_end}\t{evidence_str}"
            lines.append(line)

        return lines

    @task(task_id="write_notify_log")
    def write_notify_log(lines: list[str], execution_timestamp: str, output_dir: str = "/opt/airflow/output"):
        """
        Crea (o sobreescribe) un archivo log único por cada ejecución usando el timestamp de ejecución.
        """
        # Genera un nombre único para el archivo usando el timestamp
        output_filename = f"notify_alerts_{execution_timestamp}.log"
        output_path = os.path.join(output_dir, output_filename)
        
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, "w", encoding="utf-8") as f:
            f.write("# Archivo generado por Airflow (notify_alerts_dag)\n")
            for line in lines:
                f.write(line + "\n")
        
        return output_path

    # Extraer alertas usando el timestamp de la ejecución actual
    log_lines = extract_alerts(last_execution_date="{{ ts }}")
    
    # Usar la variable {{ ts }} de Airflow para el timestamp de la ejecución
    write_notify_log(log_lines, execution_timestamp="{{ ts_nodash }}")  # Utilizamos ts_nodash para formato sin guiones ni dos puntos
