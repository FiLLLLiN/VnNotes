CREATE TABLE IF NOT EXISTS app_users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(32) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    role VARCHAR(16) NOT NULL CHECK (role IN ('admin', 'operator', 'user')),
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    failed_attempts INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS notes (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
    note_text TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS infrastructure_nodes (
    id SERIAL PRIMARY KEY,
    node_name VARCHAR(120) UNIQUE NOT NULL,
    ip_address VARCHAR(45) NULL,
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS system_metrics (
    id SERIAL PRIMARY KEY,
    node_id INTEGER NOT NULL REFERENCES infrastructure_nodes(id) ON DELETE CASCADE,
    user_id INTEGER NULL REFERENCES app_users(id) ON DELETE SET NULL,
    cpu_percent NUMERIC(5,2) NOT NULL,
    ram_percent NUMERIC(5,2) NOT NULL,
    hdd_percent NUMERIC(5,2) NOT NULL,
    captured_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS security_logs (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NULL REFERENCES app_users(id) ON DELETE SET NULL,
    action VARCHAR(80) NOT NULL,
    details TEXT NOT NULL,
    host_name VARCHAR(120) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notes_user_id ON notes(user_id);
CREATE INDEX IF NOT EXISTS idx_metrics_node_id ON system_metrics(node_id);
CREATE INDEX IF NOT EXISTS idx_metrics_captured_at ON system_metrics(captured_at);
CREATE INDEX IF NOT EXISTS idx_logs_created_at ON security_logs(created_at);

INSERT INTO app_users(username, password_hash, role)
VALUES
('admin', md5('admin:admin123'), 'admin'),
('operator', md5('operator:operator123'), 'operator'),
('user', md5('user:user123'), 'user')
ON CONFLICT(username) DO NOTHING;