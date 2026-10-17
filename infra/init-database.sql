DO $init$
DECLARE
    app_password text := btrim(pg_read_file('/run/secrets/db_password'), E' \n\r');
BEGIN
    IF app_password !~ '^[a-f0-9]{64}$' THEN
        RAISE EXCEPTION 'Invalid application password format; use deploy-init.py';
    END IF;
    EXECUTE format('CREATE ROLE wotr LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', app_password);
END
$init$;

CREATE DATABASE wotr OWNER wotr;
