-- Corrected definition of usp_update_user. The version deployed in the
-- database is broken: its UPDATE uses an unqualified "WHERE userid = ..."
-- that is ambiguous with the RETURNS TABLE output column, so every call
-- errors at runtime. This file fixes that (WHERE users.userid = ...).
--
-- NOT CURRENTLY CALLED BY THE APP: the dc-website role lacks permission to
-- CREATE OR REPLACE FUNCTION, so UserController.UpdateUser performs the same
-- salted-hash update inline instead. If a privileged role applies this fix,
-- the controller can be switched back to calling the function.
--
-- NOTE: this function ALWAYS overwrites passwordhash, so callers must only
-- invoke it when a password change is intended.
-- Requires p_session_id to belong to an active Admin user; returns a single
-- row with userid = -1 when the session is missing, expired, or not Admin.

CREATE OR REPLACE FUNCTION public.usp_update_user(
    p_user_id     INTEGER,
    p_first_name  CHARACTER VARYING,
    p_last_name   CHARACTER VARYING,
    p_password    CHARACTER VARYING,
    p_email       CHARACTER VARYING,
    p_phone       CHARACTER VARYING,
    p_user_level  CHARACTER VARYING,
    p_permissions CHARACTER VARYING,
    p_status      CHARACTER VARYING,
    p_session_id  CHARACTER VARYING
)
RETURNS TABLE(
    userid      INTEGER,
    firstname   CHARACTER VARYING,
    lastname    CHARACTER VARYING,
    email       CHARACTER VARYING,
    phone       CHARACTER VARYING,
    userlevel   CHARACTER VARYING,
    permissions CHARACTER VARYING,
    status      CHARACTER VARYING
)
LANGUAGE plpgsql
AS $function$
DECLARE
    v_executor_id    INT;
    v_executor_level VARCHAR(50);
    v_salt           UUID;
BEGIN
    SELECT u.userid, u.userlevel INTO v_executor_id, v_executor_level
    FROM fn_security_user_by_session_id(p_session_id) u
    LIMIT 1;

    IF COALESCE(v_executor_id, 0) > 0 AND v_executor_level = 'Admin' THEN
        v_salt := gen_random_uuid();
        UPDATE users
        SET firstname    = p_first_name,
            lastname     = p_last_name,
            email        = p_email,
            phone        = p_phone,
            userlevel    = p_user_level,
            permissions  = p_permissions,
            status       = p_status,
            passwordhash = digest(p_password || v_salt::TEXT, 'sha512'),
            salt         = v_salt,
            updateddate  = NOW()
        -- users.userid must be table-qualified: the bare name is ambiguous with
        -- the RETURNS TABLE output column and makes the function error at runtime.
        WHERE users.userid = p_user_id;

        RETURN QUERY
        SELECT u.userid, u.firstname, u.lastname, u.email, u.phone, u.userlevel, u.permissions, u.status
        FROM users u WHERE u.userid = p_user_id;
    ELSE
        RETURN QUERY SELECT -1::INT, NULL::VARCHAR(55), NULL::VARCHAR(55), NULL::VARCHAR(150), NULL::VARCHAR(20), NULL::VARCHAR(50), NULL::VARCHAR(500), NULL::VARCHAR(20);
    END IF;
END;
$function$;
