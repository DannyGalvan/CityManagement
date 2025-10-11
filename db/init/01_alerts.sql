-- Crea tabla de alertas
CREATE TABLE IF NOT EXISTS alerts (
  id          BIGSERIAL PRIMARY KEY,
  source      TEXT NOT NULL,
  severity    TEXT NOT NULL CHECK (severity IN ('INFO','WARN','ERROR')),
  message     TEXT NOT NULL,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Limpia datos previos
TRUNCATE TABLE alerts;

-- Inserta 300 alertas de los últimos 60 minutos (5 por minuto)
INSERT INTO alerts (source, severity, message, created_at)
SELECT
  CASE (random()*3)::int
    WHEN 0 THEN 'api'
    WHEN 1 THEN 'worker'
    ELSE 'gateway'
  END as source,
  CASE (random()*3)::int
    WHEN 0 THEN 'INFO'
    WHEN 1 THEN 'WARN'
    ELSE 'ERROR'
  END as severity,
  'Sample alert #' || gs::text || ' from seed' as message,
  NOW() - make_interval(mins => (gs/5)) -- 5 alertas por minuto
FROM generate_series(0, 300) AS gs;

-- Índice para tiempos
CREATE INDEX IF NOT EXISTS idx_alerts_created_at ON alerts(created_at);
CREATE INDEX IF NOT EXISTS idx_alerts_severity ON alerts(severity);

