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
                               server_uuid UUID PRIMARY KEY,
                               user_uuid UUID,
                               group_uuid UUID,
                               assigned_at TIMESTAMP DEFAULT now(),
                               FOREIGN KEY (server_uuid) REFERENCES servers(uuid) ON DELETE CASCADE,
                               FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE,
                               FOREIGN KEY (group_uuid) REFERENCES groups(uuid) ON DELETE CASCADE,
                               CHECK (
                                   (user_uuid IS NOT NULL AND group_uuid IS NULL)
                                       OR (user_uuid IS NULL AND group_uuid IS NOT NULL)
                                   )
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

-- Insert default servers
INSERT INTO servers (name, description, created_at)
VALUES ('Main Server', 'Primary production server', NOW()),
       ('Backup Server', 'Backup and redundancy server', NOW()),
       ('Development Server', 'Development and testing environment', NOW());

-- Assign servers to admin user (using subquery to get admin's UUID)
INSERT INTO server_owners (server_uuid, user_uuid, group_uuid)
SELECT s.uuid, u.uuid, NULL
FROM servers s
         CROSS JOIN users u
WHERE u.username = 'admin';

-- Check inserted servers and their ownership
SELECT s.name, u.username, s.description
FROM servers s
         JOIN server_owners so ON s.uuid = so.server_uuid
         JOIN users u ON so.user_uuid = u.uuid;

-- Insert default groups
INSERT INTO groups (name, description, created_at)
VALUES ('admin', 'Administrators', NOW()),
       ('default', 'Default users', NOW());

-- Add users to their respective groups
INSERT INTO user_groups (user_uuid, group_uuid)
SELECT u.uuid, g.uuid
FROM users u, groups g
WHERE u.username = 'admin' AND g.name = 'admin';

INSERT INTO user_groups (user_uuid, group_uuid)
SELECT u.uuid, g.uuid
FROM users u, groups g
WHERE u.username = 'default_user' AND g.name = 'default';

-- Check inserted groups and memberships
SELECT name, description FROM groups;
SELECT u.username, g.name AS group_name
FROM user_groups ug
JOIN users u ON ug.user_uuid = u.uuid
JOIN groups g ON ug.group_uuid = g.uuid
ORDER BY u.username;