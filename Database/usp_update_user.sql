-- Create or replace the usp_update_user function
-- This function updates a user record and returns a status message

CREATE OR REPLACE FUNCTION usp_update_user(
    p_userid INTEGER,
    p_firstname TEXT,
    p_lastname TEXT,
    p_password TEXT,
    p_email TEXT,
    p_phone TEXT,
    p_userlevel TEXT,
    p_permissions TEXT,
    p_status TEXT,
    p_sessionid TEXT
) RETURNS TEXT AS $$
DECLARE
    v_calling_user_id INTEGER;
    v_calling_user_level TEXT;
    v_result TEXT;
BEGIN
    -- Validate the session and get the calling user's info
    SELECT userid, userlevel INTO v_calling_user_id, v_calling_user_level
    FROM users
    WHERE sessionid = p_sessionid
      AND status = 'Active';

    -- Check if session is valid
    IF v_calling_user_id IS NULL THEN
        RETURN 'Error: Invalid session';
    END IF;

    -- Check if calling user has permission (must be Admin)
    IF v_calling_user_level != 'Admin' THEN
        RETURN 'Error: Insufficient permissions';
    END IF;

    -- Check if the target user exists
    IF NOT EXISTS (SELECT 1 FROM users WHERE userid = p_userid) THEN
        RETURN 'Error: User not found';
    END IF;

    -- Update the user record
    -- Only update password if a non-empty value is provided
    IF p_password IS NOT NULL AND p_password != '' THEN
        UPDATE users SET
            firstname = p_firstname,
            lastname = p_lastname,
            password = p_password,
            email = p_email,
            phone = p_phone,
            userlevel = p_userlevel,
            permissions = p_permissions,
            status = p_status
        WHERE userid = p_userid;
    ELSE
        UPDATE users SET
            firstname = p_firstname,
            lastname = p_lastname,
            email = p_email,
            phone = p_phone,
            userlevel = p_userlevel,
            permissions = p_permissions,
            status = p_status
        WHERE userid = p_userid;
    END IF;

    -- Check if update was successful
    IF FOUND THEN
        v_result := 'Success: User updated';
    ELSE
        v_result := 'Error: Update failed';
    END IF;

    RETURN v_result;
END;
$$ LANGUAGE plpgsql;
