create or replace package WS_SET_ATTR is

  -- Public function and procedure declarations
  procedure set_attr (mssidn varchar2 , ATTR_NAME varchar2, ATTR_VALUE varchar2, rc out NUMBER);

end WS_SET_ATTR;
/
create or replace package body WS_SET_ATTR is

  --
  -- otestovani zda hodnota odpovidda zadanemu regulartnimu vyrazu
  --
  -- paramewtry:
  --   val  ... testovana hodnota
  --   patt ... regularni vyraz
  --   can_be_null ... zda muze byt vlozeby prazdny retezec
  --  
  FUNCTION check_value (val VARCHAR2, patt VARCHAR2, can_be_null BOOLEAN) RETURN BOOLEAN IS
    i NUMBER;
  BEGIN
    
    IF (can_be_null AND (val IS NULL)) THEN
      RETURN TRUE;
    END IF;
  
    i := REGEXP_INSTR(val, patt);
    
    IF ((i IS NULL) OR (i=0)) THEN
      RETURN FALSE;
    END IF;
  
    RETURN TRUE;
  END;
  

  -- Function and procedure implementations
  procedure set_attr (mssidn varchar2 , ATTR_NAME varchar2, attr_value varchar2, rc out NUMBER) is
    cnt NUMBER;
    loc_attr_id NUMBER;
    attr_value_str varchar2(255);
    subs_id NUMBER;
    p_prefix varchar2(5);
    p_nbr varchar2(16);    
  begin  
    -- === find active subs id for this MSISDN ===
    --subs_id := mon.ws_check_pwd.mssidn2cust_id (mssidn);
    
    -- parse 420 prefix
    p_prefix := SUBSTR(mssidn, 1, 3);
    p_nbr    := SUBSTR(mssidn, 4);
    
    IF ( p_prefix <> '420' ) THEN
       rc := 1;
       RETURN;
    END IF;
    
    BEGIN
      SELECT s.id INTO subs_id
      FROM cc.subs s
      INNER JOIN cc.prod p ON (p.id=s.id)
      WHERE s.acc_nbr = p_nbr AND p.prod_state IN ('A', 'H');
    EXCEPTION
      WHEN NO_DATA_FOUND THEN  
       rc := 4;
       RETURN;
    END;
    
    -- === transform emty string to NULL
    attr_value_str := attr_value;
    IF ( attr_value_str = '' ) THEN
      attr_value_str := NULL;
    END IF;
    
    -- === determine attr_id and validate value ===
    loc_attr_id := NULL;
    
    IF ATTR_NAME = 'STYPE' THEN    
      -- typ smlouvy
      --IF ((attr_value_str = '1') OR (attr_value_str = '2')) THEN
      IF (attr_value_str IN ('1', '2', '3', '4', '5')) THEN
        loc_attr_id := 357;      
      END IF;
      
    ELSIF ATTR_NAME = 'SCODE' THEN    
      -- prodejni kod (pozor je i u REC_SCODE)
      IF check_value (attr_value_str,'^[0-9]{6}[A-Z]{2}$', FALSE) THEN
        loc_attr_id := 432;
      END IF;

    ELSIF ATTR_NAME = 'SAPID' THEN    
      -- ID SAP sluzby
      IF check_value (attr_value_str,'^[0-9]{10,11}$|^0$', FALSE) THEN
        loc_attr_id := 376;
      END IF;

    ELSIF ATTR_NAME = 'REC_WHO' THEN    
      -- doporucuji koho
      IF check_value (attr_value_str,'^[0-9]{9}$', TRUE) THEN
        loc_attr_id := 516;
      END IF;

    ELSIF ATTR_NAME = 'REC_ME' THEN    
      -- doporucil mne
      IF check_value (attr_value_str,'^[0-9]{9}$', TRUE) THEN
        loc_attr_id := 515;
      END IF;

    ELSIF ATTR_NAME = 'REC_SCODE' THEN    
      -- doporucil (je i u prodejni kod)
      IF check_value (attr_value_str,'^[0-9]{6}[A-Z]{2}$', TRUE) THEN
        loc_attr_id := 492;
      END IF;
      
    ELSE
      -- invalid ATTR_NAME
      loc_attr_id := -1;      
      
    END IF;
    
    -- invalid atribute name
    IF loc_attr_id = -1 THEN
      rc := 2;
      RETURN;
    END IF;
    
    -- invalid atribute value
    IF loc_attr_id IS NULL THEN
      rc := 3;    
      RETURN;
    END IF;
    
    -- === check if row exists ===
    SELECT count(*) INTO cnt 
    FROM cc.prod_attr_value pav
    WHERE pav.prod_id = subs_id AND pav.attr_id=loc_attr_id;
    
    -- sichr
    IF cnt > 1 THEN
      rc := -1;
      RETURN;
    END IF;
    
    -- === prace s databazi ===
    
    IF (cnt = 0) THEN
      IF NOT (attr_value_str IS NULL) THEN
        -- insert
        INSERT INTO cc.prod_attr_value (prod_id, attr_id, value, eff_date, exp_date)
        VALUES (subs_id, loc_attr_id, attr_value_str, sysdate, null);
        
      ELSE
        -- null value, not existing row ...
        NULL;
      END IF;
    
    ELSE
      IF NOT (attr_value_str IS NULL) THEN
        -- update
        UPDATE cc.prod_attr_value pav SET pav.eff_date = sysdate, pav.value = attr_value_str
        WHERE pav.prod_id = subs_id AND pav.attr_id = loc_attr_id;
        
      ELSE  
        -- delete - zatiom to nedelame
        rc := -2;
        RETURN;
        
      END IF;
    END IF;
    
    rc := 0;
    RETURN;
  end;

  
begin
  null;
end WS_SET_ATTR;
/
