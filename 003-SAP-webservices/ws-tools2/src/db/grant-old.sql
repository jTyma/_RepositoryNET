--
-- vytvoreni uzivatelu pro web services SAPu
--

create user CRMWS
  identified by CENSORED
  default tablespace USERS
  temporary tablespace TEMP
  profile DEFAULT;

create user CRMWS_WEB
  identified by CENSORED
  default tablespace USERS
  temporary tablespace TEMP
  profile DEFAULT;

-- aby se mohl pripojovat
grant connect to CRMWS;
grant resource to CRMWS;

-- Grant/Revoke system privileges 
grant create session to CRMWS;
grant unlimited tablespace to CRMWS;

-- aby se mohl pripojovat
grant connect to CRMWS_WEB;
grant resource to CRMWS_WEB;

-- Grant/Revoke system privileges 
grant create session to CRMWS_WEB;
grant unlimited tablespace to CRMWS_WEB;

-- role se nedelaji, protoze se jen spousteji procedury
-- a ty vraceni cursory, takze figle s view a rolema nejsou potreba

-- selecty do tabulek
grant select on cc.prod_his to crmws;
grant select on  cc.PROD_ATTR_VALUE_HIS to crmws;
grant select on  cc.attr to crmws;
grant select on cc.acct to crmws;
grant select on cc.cust_attr_value to crmws;
grant select on cc.prod_state to crmws;
grant select on cc.bill to crmws;
grant select on cc.billing_cycle to crmws;

