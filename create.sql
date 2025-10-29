-- Enable pgcrypto extension for UUID generation and hashing
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Drop tables if they exist to start fresh
/*
DROP TABLE IF EXISTS service_owners CASCADE;
DROP TABLE IF EXISTS server_owners CASCADE;
DROP TABLE IF EXISTS services CASCADE;
DROP TABLE IF EXISTS servers CASCADE;
DROP TABLE IF EXISTS user_groups CASCADE;
DROP TABLE IF EXISTS groups CASCADE;
DROP TABLE IF EXISTS users CASCADE;
*/  

-- Users
CREATE TABLE users (
                       uuid UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                       username VARCHAR NOT NULL,
                       email VARCHAR NOT NULL UNIQUE,
                       firstname VARCHAR,
                       middlename VARCHAR,
                       lastname VARCHAR,
                       password_hash VARCHAR NOT NULL,
                       created_at TIMESTAMP DEFAULT now()
);

-- Groups
CREATE TABLE groups (
                        uuid UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        name VARCHAR NOT NULL,
                        description TEXT,
                        created_at TIMESTAMP DEFAULT now()
);

-- Users <-> Groups (many-to-many)
CREATE TABLE user_groups (
                             user_uuid UUID NOT NULL,
                             group_uuid UUID NOT NULL,
                             assigned_at TIMESTAMP DEFAULT now(),
                             PRIMARY KEY (user_uuid, group_uuid),
                             FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE,
                             FOREIGN KEY (group_uuid) REFERENCES groups(uuid) ON DELETE CASCADE
);

-- Servers
CREATE TABLE servers (
                         uuid UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                         name VARCHAR NOT NULL,
                         description TEXT,
                         created_at TIMESTAMP DEFAULT now()
);

-- Services
CREATE TABLE services (
                          uuid UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                          server_uuid UUID NOT NULL,
                          name VARCHAR NOT NULL,
                          type SMALLINT NOT NULL, -- small integer for service type
                          description TEXT,
                          created_at TIMESTAMP DEFAULT now(),
                          FOREIGN KEY (server_uuid) REFERENCES servers(uuid) ON DELETE CASCADE
);

-- Server owners (many-to-many: users & groups)
CREATE TABLE server_owners (
                               server_uuid UUID NOT NULL,
                               user_uuid UUID NULL,
                               group_uuid UUID NULL,
                               assigned_at TIMESTAMP DEFAULT now(),
                               PRIMARY KEY (server_uuid, user_uuid, group_uuid),
                               FOREIGN KEY (server_uuid) REFERENCES servers(uuid) ON DELETE CASCADE,
                               FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE,
                               FOREIGN KEY (group_uuid) REFERENCES groups(uuid) ON DELETE CASCADE
);

-- Service owners (many-to-many: users & groups)
CREATE TABLE service_owners (
                                service_uuid UUID NOT NULL,
                                user_uuid UUID NULL,
                                group_uuid UUID NULL,
                                assigned_at TIMESTAMP DEFAULT now(),
                                PRIMARY KEY (service_uuid, user_uuid, group_uuid),
                                FOREIGN KEY (service_uuid) REFERENCES services(uuid) ON DELETE CASCADE,
                                FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE,
                                FOREIGN KEY (group_uuid) REFERENCES groups(uuid) ON DELETE CASCADE
);

-- Insert default users with hashed passwords
INSERT INTO users (username, email, firstname, middlename, lastname, password_hash, created_at)
VALUES
    ('admin', 'admin@example.batacek.eu', 'Admin', NULL, 'User', encode(digest('admin123', 'sha256'), 'hex'), NOW()),
    ('default_user', 'default@example.batacek.eu', 'Default', NULL, 'User', encode(digest('user123', 'sha256'), 'hex'), NOW());

-- Check inserted users
SELECT * FROM users;