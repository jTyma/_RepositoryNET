CREATE OR REPLACE PACKAGE "WS_FUNCTIONS_DEV" is

  -- Author  : AVI
  -- Created : 3.6.2008 14:39:54
  -- Purpose : 
  
  FUNCTION mssidn2cust_id ( p_mssidn VARCHAR2 ) RETURN NUMBER;
  
  PROCEDURE getAcctInfo 
  (
      p_acct_id NUMBER, 
      p_mssidn VARCHAR2, 
      
      p_InvNbr            OUT VARCHAR2,
      p_InvAmount         OUT VARCHAR2,
      p_InvDueDate        OUT VARCHAR2,
      p_ActualBalance     OUT VARCHAR2,
      p_PaymentAmt        OUT VARCHAR2,
      p_PaymentDate       OUT VARCHAR2,
      p_BillCycleDateFrom OUT VARCHAR2,
      p_BillCycleDateTo   OUT VARCHAR2,
      
      p_result OUT NUMBER
  );
  
  procedure GET_PROFILE (in_MSISDN in cc.subs.acc_nbr%TYPE,
                         in_SUBS_ID in cc.subs.id%TYPE,
                         p_acct_type out varchar2,
                         p_acct_id out cc.acct.id%TYPE,
                         p_msisdn out cc.subs.acc_nbr%TYPE,
                         p_subs_state out cc.prod.prod_state%TYPE,
                         p_tarif_id out cc.subs.price_plan_id%TYPE,
                         p_tarif_desc out cc.price_plan.price_plan_name%TYPE,
                         p_actual_credit out number,
                         p_last_credit_d out date,
                         p_last_credit_a out number,
                         p_last_debit_d out date,
                         p_last_debit_a out number,
                         p_subs_used out integer,
                         p_rc out integer);  

  --                         
  -- zjisteni aktualniho dluhu (pro postpaidoveho zakaznika)
  -- jedna se o obalku k volani funkce 
  --   collection.cc_collection_common.get_due_amount
  --
  PROCEDURE get_account_debt (p_acct_id NUMBER, p_due OUT NUMBER, p_rc OUT NUMBER, p_msg OUT VARCHAR2);
 
  -- =====================================================================
  --   zjisneni zakaznickeho cisla pres kontaktni telefon
  --   vstupem je cislo ve tvaru NNN NNN NNN (bez +420)
  --
  --   vrai to acct_id, pokud se nekdo takovy jednoznacne najde.
  --   pokud to neni jednoznacne, ta to vrati NULL stejne jako by 
  --   to nebylo nalezeno
  -- =====================================================================
  FUNCTION cotact_phone2acct_id (p_msisdn VARCHAR2) RETURN NUMBER;
  
  --
  -- funkce na ziskani data aktivace pro dane subs_id
  --
  FUNCTION activation_date (p_subs_id NUMBER) RETURN DATE;
  
  PROCEDURE apply_contact_phone (
    p_acct_id NUMBER, p_msisdn VARCHAR2,
    p_acct_id_fixed OUT NUMBER, p_msisdn_fixed OUT VARCHAR2);

end WS_FUNCTIONS_DEV;

 

 
/
CREATE OR REPLACE PACKAGE BODY "WS_FUNCTIONS_DEV" is

  --
  -- procedura pro ziskani udaju o stavu FUPKY
  --   p_susb_id ... kdo
  --   p_is_over_quota ... 1/0 (ano/ne), -1 chyba
  --
  --   p_fup_day ... kdy se v mesici resetuje FUPka 
  --                 > 0  - den v mesici kdy se resetuje
  --                 = 0  - nelze takovy den urcit (asi denni limit nebo bez FUPky)
  --                 = -1 - chyba
  --
  --   p_rc     ... zda se to povedlo (0 Ok, jinak chyba)
  --
  PROCEDURE get_fup_state (p_susb_id NUMBER, p_is_over_quota OUT NUMBER, p_fup_day OUT NUMBER, p_rc OUT NUMBER) IS
    l_imsi VARCHAR2 (50);
    l_profile_name VARCHAR2 (50);
    l_allot_profile_name VARCHAR2 (50);
    l_rc NUMBER;
    l_msg VARCHAR2 (1000);
    l_rx_buff VARCHAR2 (20);
  BEGIN
    
    p_rc := -1;
    p_is_over_quota := -1;
    p_fup_day := -1;
    
    -- pohledame IMSI
    BEGIN
      SELECT sc.imsi INTO l_imsi
      FROM cc.subs s
      INNER JOIN cc.acc_nbr an on (s.acc_nbr = an.acc_nbr)
      INNER JOIN cc.sim_nbr sn on (an.id = sn.acc_nbr_id)
      INNER JOIN cc.sim_card sc on (sn.sim_card_id = sc.id)
      INNER JOIN cc.prod pr on (pr.id=s.id)
      WHERE sn.state = 'A' AND pr.prod_state IN ('A','D','E','H') AND  s.id=p_susb_id;
    EXCEPTION
      when others then
        return;
    END;
    
    -- pokusime se najit v nastavenych FUPkach
    BEGIN
      SELECT fis.profile_name INTO l_profile_name
      FROM mon.fup_imsi_state fis 
      WHERE fis.imsi = l_imsi AND (NOT (fis.profile_name IS NULL));
    EXCEPTION
      when others then
        return;    
    END;
    
    -- zeptat se v allotu
    begin
      mon.fup_control.get_profile (l_imsi, l_allot_profile_name, l_rc, l_msg);
    EXCEPTION
      when others then
        return;    
    end;
    
    IF l_rc <> 0 THEN
      return;          
    END IF;
    
    -- je-li  na konci OQ, tak je zafupovanej
    IF (upper(substr(l_allot_profile_name, length(l_allot_profile_name)-1, 2)) = 'OQ') THEN
      p_is_over_quota := 1;
    ELSE
      p_is_over_quota := 0;                  
    END IF;
    
    -- den otoceni FUPky (posledni dve cisilice)
    l_rx_buff := REGEXP_SUBSTR(l_profile_name, '\d{1,2}$');
    IF NOT l_rx_buff IS NULL THEN
      p_fup_day := to_number(l_rx_buff);
    ELSE
      p_fup_day := 0;
    END IF;    
    
    p_rc := 0;
  END;  


  --
  -- funkce na ziskani data aktivace pro dane subs_id
  --
  FUNCTION activation_date (p_subs_id NUMBER) RETURN DATE IS
    l_date DATE;
  BEGIN
    l_date := NULL;
    
    -- nejpravnejsi metoda, ale ta nema vsude data - vzit to z atributu
    -- na produktu. Bohuzle ne vsude tento atribut je
    BEGIN
      SELECT to_date (pav.value, 'YYYY-MM-DD') INTO l_date
      FROM cc.prod_attr_value pav
      WHERE 
        pav.attr_id=695 AND -- atribut CONTRACT_DATE
        pav.prod_id = p_subs_id AND 
        pav.exp_date IS NULL;
    EXCEPTION 
      WHEN others THEN
        l_date := NULL;
    END;
    
    -- bajecne mame to
    IF NOT (l_date IS NULL) THEN
      RETURN l_date;
    END IF;
    
    -- najdeme kdy naposledy prestal platit stav 'H' (predaktivivano)
    BEGIN
      SELECT MIN(exp_date) INTO l_date
      FROM
      (SELECT ph.exp_date 
        FROM cc.prod_his ph
        WHERE ph.prod_type='M' AND ph.prod_state='H' and ph.id=p_subs_id
        union all
        SELECT pa.exp_date 
        FROM cc.prod_arch pa
        WHERE pa.prod_type='M' AND pa.prod_state='H' and pa.id=p_subs_id
      );      
    
    EXCEPTION 
      WHEN others THEN
        l_date := NULL;
    END;

    -- bajecne mame to
    IF NOT (l_date IS NULL) THEN
      RETURN l_date;
    END IF;    

    -- todle je posledni a nejmene presna varianta, ukazuje to, kdy
    -- se zakaznik stal naposledy aktivni (tj. pokud dojde k blokaci
    -- a nasledne aktivaci, tak to vrat datum posledni aktivace
    BEGIN
      SELECT p.prod_state_date AS created_date INTO l_date
      FROM cc.subs s
      INNER JOIN cc.prod p on (p.id=s.id)
      WHERE p.prod_state in ('A','D','E','H') AND s.id=p_subs_id;      
    EXCEPTION 
      WHEN others THEN
        l_date := NULL;
    END;
    
    RETURN l_date;
  END;

  -- =====================================================================
  -- prevod z cisla na zakanika (pouze mezi aktivnimi zakazniky)
  --
  --  p_mssidn ... MSISDN
  --
  --  navratova hodnota: CUST_ID (cizlo zakanika)
  -- =====================================================================
  FUNCTION mssidn2cust_id ( p_mssidn VARCHAR2 ) RETURN NUMBER IS
    l_cust_id NUMBER;
  BEGIN

    BEGIN  
      SELECT s.cust_id INTO l_cust_id
      FROM cc.subs s 
      INNER JOIN cc.prod p ON (p.id=s.id)
      WHERE s.acc_nbr=p_mssidn AND p.prod_state IN ('A','D','E','H');
    EXCEPTION
      WHEN NO_DATA_FOUND THEN  
        l_cust_id := null;
    END;
    
    RETURN l_cust_id;
  END;
  
  -- =====================================================================
  --   zjisneni zakaznickeho cisla pres kontaktni telefon
  --   vstupem je cislo ve tvaru NNN NNN NNN (bez +420)
  --
  --   vrai to acct_id, pokud se nekdo takovy jednoznacne najde.
  --   pokud to neni jednoznacne, ta to vrati NULL stejne jako by 
  --   to nebylo nalezeno
  -- =====================================================================
  FUNCTION cotact_phone2acct_id (p_msisdn VARCHAR2) RETURN NUMBER IS
    l_acct_id NUMBER;
  BEGIN
    l_acct_id := NULL;
    BEGIN
      SELECT a.id INTO l_acct_id
      FROM cc.cust_attr_value cav
      INNER JOIN cc.acct a ON (a.cust_id = cav.cust_id)
      WHERE cav.cust_attr_db_id = 333 -- kontaktni telefon
            AND (
              (cav.attr_value=p_msisdn) OR 
              (cav.attr_value='420' || p_msisdn) OR
              (cav.attr_value='+420' || p_msisdn)
      );
    EXCEPTION
      WHEN others THEN
        l_acct_id := NULL;
    END;
    RETURN l_acct_id;
  END;
  
  
  -- TODO doc
  PROCEDURE apply_contact_phone (
    p_acct_id NUMBER, p_msisdn VARCHAR2,
    p_acct_id_fixed OUT NUMBER, p_msisdn_fixed OUT VARCHAR2)
  IS
    l_opt_part  VARCHAR2(30);
    l_misidn_part VARCHAR2(30);
    l_new_misidn_part VARCHAR2(30);
    l_idx NUMBER;
    l_count NUMBER;
    l_acct_id NUMBER;
  BEGIN
    -- zakladni kopirovani
    p_acct_id_fixed := p_acct_id;
    p_msisdn_fixed := p_msisdn;
    
    -- pokud se hleda podle cisla, tak overime, zda je to cislo neterminovaneho 
    -- produktu a pokud neni, tak zkusime najit nejaky aktivni produkt a tam tonahradit
    -- do toho je zde snaha prenest pripadne pretizene parametry
    IF NOT (p_msisdn IS NULL) THEN
      l_idx := INSTR(p_msisdn, ':');
      
      IF l_idx > 0 THEN
        l_opt_part    := TRIM(SUBSTR (p_msisdn, l_idx +1));
        l_misidn_part := TRIM(SUBSTR (p_msisdn, 1, l_idx - 1));
      ELSE
        l_opt_part := NULL;
        l_misidn_part := p_msisdn;
      END IF;
      
      -- pohledame, zda MISIDN je akticni zakaznik
      SELECT count(*) INTO l_count
      FROM selfcare.v_active_prod p 
      WHERE p.acc_nbr = l_misidn_part;
      
      IF l_count > 0 THEN
        -- zadany telefon je aktivni produkt
        -- nechame to tak byt
        RETURN;
      END IF;
      
      -- neni to aktivni telefon, zkusime kontaktni telefon
      l_acct_id := cotact_phone2acct_id (l_misidn_part);
      
      -- nenaslo se to, nechame to jak to je
      IF l_acct_id IS NULL THEN        
        RETURN;
      END IF;
      
      -- pokud se to nepta na zadne pretizene parametry, tak
      -- pouzijeme ziskane l_acct_id
      IF (l_opt_part IS NULL) THEN
        p_acct_id_fixed := l_acct_id;
        p_msisdn_fixed := NULL;
        RETURN;
      END IF;
      
      -- je na tomto acct_id nejaky aktivni produk?
      SELECT count(*) INTO l_count
      FROM selfcare.v_active_prod p 
      WHERE p.acct_id = l_acct_id;
      
      IF l_count = 0 THEN
        -- zadny produkt, nechame to jak to je
        RETURN;
      END IF;
      
      IF l_count = 1 THEN
        -- jeden produkt, vezmene jeho cialo a pouzijeme jej
        SELECT p.acc_nbr INTO l_new_misidn_part
        FROM selfcare.v_active_prod p 
        WHERE p.acct_id = l_acct_id;
        
        p_acct_id_fixed := NULL;
        p_msisdn_fixed := l_new_misidn_part || ':' || l_opt_part;               
        
        RETURN;
      END IF;
      

      IF l_count > 1 THEN
        -- bereme prvniho, nejcersvejsiho
        select acc_nbr INTO l_new_misidn_part
        from
        (select rownum,p.acc_nbr from selfcare.v_active_prod p
        where p.acct_id = l_acct_id
        order by created_date desc)
        where rownum=1;

        p_acct_id_fixed := NULL;
        p_msisdn_fixed := l_new_misidn_part || ':' || l_opt_part;               
        
        RETURN;              
      END IF;        
    END IF;       
  END;
  
  -- =====================================================================
  -- prevod z cisla na zakanika na cislo uctu zakaznika
  --
  --  p_cust_id ... CUST_ID (cislo zakaznika)
  --
  -- vraci to cislo uctu ACCT_ID
  -- =====================================================================
  FUNCTION cust_id2acct_id ( p_cust_id NUMBER ) RETURN NUMBER IS
    l_acct_id NUMBER;
  BEGIN

    BEGIN  
      SELECT a.id INTO l_acct_id
      FROM cc.acct a
      WHERE a.cust_id = p_cust_id;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN  
        l_acct_id := null;
    END;
    
    RETURN l_acct_id;
  END;
  
  -- =====================================================================
  -- prevod z telefonu na cislo uctu zakanika
  --
  --  p_mssidn .... MSISDN
  --
  -- navratova hodnota: cislo uctu
  -- =====================================================================
  FUNCTION mssidn2acct_id ( p_mssidn VARCHAR2 ) RETURN NUMBER IS
  BEGIN
    RETURN cust_id2acct_id (mssidn2cust_id (p_mssidn));
  END;
  
  -- ziskani pozadovane vlastnosti pro zakaznika
  PROCEDURE get_accout_property (
    p_cust_id NUMBER,    
    p_acct_id NUMBER, 
    p_subs_id NUMBER,    
    p_key VARCHAR2,
    p_value OUT VARCHAR2,
    p_rc OUT NUMBER,
    p_msg OUT VARCHAR2   
  )
  IS
    l_count NUMBER;
    l_bc_id NUMBER;
    l_date DATE;
    l_date2 DATE;
    
    l_rc NUMBER;
    l_oq NUMBER;
    l_qday NUMBER;
  BEGIN
    
    IF p_key = 'PAYMENT_METHOD' THEN
      p_msg := NULL;
      
      -- pohledame hledanou metodu
      BEGIN
        SELECT cav.attr_value INTO p_value
        FROM cc.cust_attr_value cav
        INNER JOIN cc.acct a ON (a.cust_id=cav.cust_id)
        WHERE a.id = p_acct_id AND cav.cust_attr_db_id=277; -- Platebn� metoda        
      EXCEPTION 
        WHEN NO_DATA_FOUND THEN
          p_value := NULL;
      END;
              
      p_rc := 0;
      RETURN;      
    END IF;
    
    IF p_key = 'SOLD_DEBTOR' THEN
      p_msg := NULL;
      
      -- pohledame hledanou metodu
      BEGIN
        SELECT count(*) INTO l_count
        FROM mon.blacklist_addition ba
        WHERE ba.acct_id =  p_acct_id AND ba.note='Pohledavka odprodana vymahacum';        
      EXCEPTION 
        WHEN NO_DATA_FOUND THEN
          p_value := 'N';
      END;
      
      IF l_count > 0 THEN
        p_value := 'Y';
      ELSE
        p_value := 'N';
      END IF;
              
      p_rc := 0;
      RETURN;      
    END IF;
    
    -- upominka v poslednim bill cyklu
    IF p_key = 'UPOMINKA' THEN
        -- pohledame posledni bill cyklus  
        
        BEGIN
          SELECT billing_cycle_id INTO l_bc_id
          FROM
          (
            SELECT b.billing_cycle_id, row_number() over (order by b.cycle_end_date desc) r
            FROM cc.billing_cycle b 
            WHERE b.state='C' AND b.billing_cycle_type_id IN (
               SELECT a.billing_cycle_type_id 
               FROM cc.acct a 
               WHERE a.id=p_acct_id) 
          ) WHERE r=1;
        EXCEPTION
          WHEN no_data_found THEN
            l_bc_id := NULL;
        END;
        
        -- sichr
        IF l_bc_id IS NULL THEN
          p_value := 'N';      
          p_rc := 0;
          RETURN;          
        END IF;
        
        -- pohledame kolik toho je
select nvl(count(*),0) cnt into l_count
  from sale_payment_item spi, sale_payment sp, subs s, billing_cycle bc
 where spi.sale_item_id = 8
   and sp.sale_payment_id = spi.sale_payment_id
   and sp.state = 'B'
   and sp.subs_id = s.id 
   and s.acct_id = p_acct_id
   and bc.billing_cycle_id = l_bc_id
   and sp.state_date-1 between bc.cycle_begin_date and bc.cycle_end_date;

       
        -- je tam upominka?
        IF l_count > 0 THEN
          p_value := 'Y';      
        ELSE
          p_value := 'N';      
        END IF;
 
        p_rc := 0;
        RETURN;
    END IF;
    
    -- je to nova sluzba (datum aktivace neni starsi nez jeden den
    IF p_key = 'IS_NEW' THEN
        
        l_date := activation_date (p_subs_id);
        
        -- sichr, nemelo by nastatt
        IF l_date IS NULL THEN
          p_value := 'N';      
          p_rc := 0;
          RETURN;          
        END IF;

        -- jak je to dlouho aktivovane
        l_count := trunc(sysdate) - trunc(l_date);
          
        -- aktivovano dneska nebo vcera
        IF (l_count <= 1) THEN
          p_value := 'Y';      
        ELSE
          p_value := 'N';      
        END IF;
        
        p_rc := 0; 
        RETURN;      
    END IF;

    -- zda je to 2 az 15 dnu    
    IF p_key = 'IS_NEW_15' THEN
        
        l_date := activation_date (p_subs_id);
        
        -- sichr, nemelo by nastatt
        IF l_date IS NULL THEN
          p_value := 'N';      
          p_rc := 0;          
          RETURN;          
        END IF;

        -- jak je to dlouho aktivovane
        l_count := trunc(sysdate) - trunc(l_date);
          
        -- aktivovano dneska nebo vcera
        IF (l_count <= 15) AND (l_count >= 2)THEN
          p_value := 'Y';      
        ELSE
          p_value := 'N';      
        END IF;
        
        p_rc := 0;  
        RETURN;      
    END IF;

    -- kolik dnu to je
    IF p_key = 'ACTIVE_DAYS' THEN
        
        l_date := activation_date (p_subs_id);
        
        -- sichr, nemelo by nastatt
        IF l_date IS NULL THEN
          p_value := NULL;
          p_rc := 0;          
          RETURN;          
        END IF;

        -- jak je to dlouho aktivovane
        l_count := trunc(sysdate) - trunc(l_date);
          
        p_value := l_count;
        p_rc := 0;        
        RETURN;      
    END IF;
    
    -- zda byla posledni faktura po splatnosti
    IF p_key = 'POZDNI_PLATBA' THEN
      
      -- splatnost posledni faktury
      BEGIN
        SELECT max(bc.debt_date) INTO l_date
        FROM cc.bill b
        INNER JOIN cc.billing_cycle bc ON (bc.billing_cycle_id=b.billing_cycle_id)
        WHERE b.acct_id=p_acct_id;
      EXCEPTION 
        WHEN others THEN
          l_date := NULL;
      END;
      
      -- nemame jeste fakturu
      IF l_date IS NULL THEN
        p_rc := 0;
        p_value := 'N';
        RETURN;
      END IF;
      
      -- kdy prisly posledni penize
      BEGIN
        SELECT trunc(max(ab.created_date)) INTO l_date2
        FROM cc.acct_book ab 
        WHERE ab.acct_book_type IN ('P') AND ab.acct_id=p_acct_id AND ab.charge < 0;
      EXCEPTION 
        WHEN others THEN
          l_date2 := NULL;
      END;
            
      -- zadne penize jeste nikdy neprisly
      IF l_date2 IS NULL THEN
        
        -- pokud jiz uplynula splatnost
        IF l_date < sysdate THEN
          -- jiz po splatnosti a zadne penize
          p_rc := 0;
          p_value := 'Y';
          RETURN;
        ELSE
          -- zadne penize, ale jeste neni po splatnosti 
          p_rc := 0;
          p_value := 'N';          
          RETURN;
        END IF;
        
      ELSE
        -- jiz nejake penize prisly
        
        -- posledni penize prisly pred splatnosti
        IF l_date2 <= l_date THEN
          p_rc := 0;
          p_value := 'N';          
          RETURN;
        ELSE
          -- penize prisly po splatnosti
          p_rc := 0;
          p_value := 'Y';
          RETURN;
        END IF;
      END IF;    
      
    END IF;
    
    -- zda na poseldni fakture byl nejaky zaporny prebalance
    -- tj. zda tam byl nejaky stary dluh
    IF p_key = 'FAKT_DVE_CASTKY' THEN
      SELECT count(*) INTO l_count
      FROM 
      (
        SELECT b.pre_balance, b.due, row_number() over (ORDER BY b.bill_id DESC) r
        FROM cc.bill b
        INNER JOIN cc.billing_cycle bc on (bc.billing_cycle_id=b.billing_cycle_id)
        WHERE b.acct_id = p_acct_id
      )
      WHERE r=1 and pre_balance >=10000; -- prebalance kladne znamena dluh, je to v halerich (> 100 Kc)

      IF l_count = 0 THEN
        p_rc := 0;
        p_value := 'N';          
        RETURN;
      ELSE
        p_rc := 0;
        p_value := 'Y';
        RETURN;
      END IF;
      
    END IF;
    
    -- udaje o FUPce
    IF p_key = 'FUP' THEN
      IF p_subs_id IS NULL THEN
        p_rc := 0;
        p_value := -1;
        RETURN;
      END IF;
      
      get_fup_state(p_subs_id, l_oq, l_qday, l_rc);
      
      -- nepovedlo se to
      IF l_rc <> 0 OR l_oq <0 OR l_qday <0 THEN
        p_rc := 0;
        p_value := -2;        
        RETURN;
      END IF;
      
      IF l_oq > 0 THEN
        p_rc := 0;
        p_value := l_qday + 100;
      ELSE
        p_value := l_qday;
      END IF;
      
      RETURN;
      
      
    END IF;
    
    -- do jake skupiny zakazniku to patri?
    IF p_key = 'CUST_GRP' THEN
        -- hleda se parametr CUSTOMER_GROUP a hodnota v nemu ulozena (do jake
        -- skupiny zakazniku tendle zakaznik patri)
        BEGIN
          select to_number(cav.attr_value) into p_value
          from cc.cust_attr_value cav
          inner join cc.attr ca on (ca.id = cav.cust_attr_db_id)
          where ca.attr_code='CUSTOMER_GROUP' 
          and cav.cust_id=p_cust_id;
        EXCEPTION
          WHEN NO_DATA_FOUND THEN
            cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_PROFILE', 'Warning: cust param CUSTOMER_GROUP not set for cust_id=' || p_cust_id ||', using value 1');
            p_value := 1;
        END;
        
        p_rc := 0;    
        RETURN;        
    END IF;
    
    p_rc := -1;
    p_msg := 'unknown property';
  EXCEPTION
    WHEN OTHERS THEN
     p_rc := -2;
     p_msg := sqlerrm;    
  END;
    
  
  
  -- procedura na zpracovani acctInfo
  PROCEDURE getAcctInfo_opt_wrapper
  (
      p_msisdn VARCHAR2,       
      p_InvNbr            OUT VARCHAR2,
      p_InvAmount         OUT VARCHAR2,
      p_InvDueDate        OUT VARCHAR2,
      p_ActualBalance     OUT VARCHAR2,
      p_PaymentAmt        OUT VARCHAR2,
      p_PaymentDate       OUT VARCHAR2,
      p_BillCycleDateFrom OUT VARCHAR2,
      p_BillCycleDateTo   OUT VARCHAR2,
      
      p_result OUT NUMBER
  ) IS  
    l_idx NUMBER;
    l_opt VARCHAR2(50);
    l_msisdn VARCHAR2(20);
    l_search_contact_phone BOOLEAN;
    l_acct_id NUMBER;
    
    l_rc NUMBER;    
    l_msg VARCHAR2(200);
    l_value VARCHAR2(400);
    l_subs_id NUMBER;
    l_cust_id NUMBER;
  BEGIN    
    
    -- resetace
    p_InvNbr            := NULL;
    p_InvAmount         := NULL;
    p_InvDueDate        := NULL;
    p_ActualBalance     := NULL;
    p_PaymentAmt        := NULL;
    p_PaymentDate       := NULL;
    p_BillCycleDateFrom := NULL;
    p_BillCycleDateTo   := NULL;
    
    -- prozkoumame option, nalezneme pripadny kontaktin telefon    
    l_idx := INSTR(p_msisdn, ':');
    
    IF NOT (l_idx > 0 ) THEN
      -- no options parametr
      p_result := -100;
      RETURN;
    END IF;

    -- rozdelime cislo a options
    l_opt := TRIM(SUBSTR (p_msisdn, l_idx +1));
    l_msisdn := TRIM(SUBSTR (p_msisdn, 1, l_idx - 1));
      
    -- pripadne mezery
    l_opt := REPLACE (l_opt, ' ');

    -- priznak, zda hledat v kontaktich telefonech      
    IF INSTR (l_opt, 'C,') = 1 THEN
      l_opt := SUBSTR (l_opt, 3);
      l_search_contact_phone := TRUE;
    ELSE
      l_search_contact_phone := FALSE;
    END IF;
      
    -- konecne to pohledame o jakeho zakaznika se jedna
    BEGIN
      SELECT s.acct_id, s.id, s.cust_id INTO l_acct_id, l_subs_id, l_cust_id
      FROM cc.subs s       
      INNER JOIN cc.prod p ON (p.id=s.id)
      WHERE s.acc_nbr=l_msisdn AND p.prod_state IN ('A','D','E','H');      
    EXCEPTION
      WHEN no_data_found THEN
        l_acct_id := NULL;
        l_subs_id := NULL;
        l_cust_id := NULL;
    END;
        
    -- nemame, zkusit dohledat dle kontaktiho cisla
    IF l_acct_id IS NULL THEN
      IF NOT l_search_contact_phone THEN
        p_result := -101;
        RETURN;
      END IF;
      
      -- pohledat dle kontaktinho telefonu
      l_acct_id := cotact_phone2acct_id (l_msisdn);
      l_subs_id := NULL;
       
      IF l_acct_id IS NULL THEN
        -- ani todle se nepovedlo
        p_result := -102;
        RETURN;
      END IF;      
      
      -- dohledat cus_id z acct_id, mame-li zakaznika z kontaktniho telefonu
      SELECT a.cust_id INTO l_cust_id
      FROM cc.acct a WHERE a.id = l_acct_id;
      
    END IF;
    
    -- mame vlastnost, mame zakaznika, takze to konecne muzeme vypocitat
    get_accout_property (l_cust_id,l_acct_id, l_subs_id, l_opt, l_value, l_rc, l_msg);
    
    IF (l_rc <> 0 ) THEN
      p_result := -500 + l_rc;
      RETURN;
    END IF;
      
    p_PaymentAmt := l_value;
    p_result := 0;
  END;  
  -- =====================================================================
  -- funkce pro API getAcctInfo
  --
  -- vstup:
  --   p_acct_id ... cislo uctu
  --   p_mssidn .... MSISDN
  --
  -- je treba zadat bud p_mssidn nebo p_acct_id
  --
  -- vystup:
  --   p_InvNbr            cislo posledni faktury
  --   p_InvAmount         castka posledni faktury
  --   p_InvDueDate        splatnost posledni faktury
  --   p_ActualBalance     aktualni vyse dluhu / preplatku
  --   p_PaymentAmt        posledni zaznamenana platba
  --   p_PaymentDate       datum posledni platby
  --   p_BillCycleDateFrom od kdy zacina aktualni billcyklus pro uzivatele
  --   p_BillCycleDateTo   do kdy zacina aktualni billcyklus pro uzivatele
  --   p_result OUT NUMBER cislo indikujici, zda se to povedlo
  --       0 ... v poradku
  --      -1 ... nenalezen zakaznik
  --
  -- =====================================================================
  PROCEDURE getAcctInfo 
  (
      p_acct_id NUMBER, 
      p_mssidn VARCHAR2, 
      
      p_InvNbr            OUT VARCHAR2,
      p_InvAmount         OUT VARCHAR2,
      p_InvDueDate        OUT VARCHAR2,
      p_ActualBalance     OUT VARCHAR2,
      p_PaymentAmt        OUT VARCHAR2,
      p_PaymentDate       OUT VARCHAR2,
      p_BillCycleDateFrom OUT VARCHAR2,
      p_BillCycleDateTo   OUT VARCHAR2,
      
      p_result OUT NUMBER
  ) IS
  
    l_acct_id NUMBER;
    l_acct_id_fixed NUMBER;
    l_msisdn VARCHAR2(20);
    l_msisdn_fixed VARCHAR2(50);
    l_sipo_count NUMBER;
  BEGIN 
    apply_contact_phone (
      p_acct_id, p_mssidn,
      l_acct_id_fixed, l_msisdn_fixed);
--    l_msisdn_fixed := p_mssidn;
--    l_acct_id_fixed := p_acct_id;
    
    -- pokud hledame pres
  
    -- pohledat options, pokud existuji
    IF NOT (l_msisdn_fixed IS NULL) THEN
      -- je v telefonim cisle naznak options?
      IF INSTR (l_msisdn_fixed, ':') > 0 THEN
        getAcctInfo_opt_wrapper(
          l_msisdn_fixed,       
          p_InvNbr,
          p_InvAmount,
          p_InvDueDate,
          p_ActualBalance,
          p_PaymentAmt,
          p_PaymentDate,
          p_BillCycleDateFrom,
          p_BillCycleDateTo,      
          p_result);
        
         --RETURN;
        l_msisdn := SUBSTR (l_msisdn_fixed, 0, INSTR (l_msisdn_fixed, ':') - 1);
      ELSE
        l_msisdn := l_msisdn_fixed;
      END IF;
    END IF;
  
    -- o koho jde
    IF (l_acct_id_fixed IS NULL) THEN
      l_acct_id := mssidn2acct_id (l_msisdn);
    ELSE
      -- zkontrolovat zda zadane acccount id existuje
      BEGIN
        SELECT a.id INTO l_acct_id
        FROM cc.acct a 
        WHERE a.id = l_acct_id_fixed;
      EXCEPTION
        WHEN NO_DATA_FOUND THEN  
          l_acct_id := NULL;
      END;
    END IF;
    
    IF (l_acct_id IS NULL) THEN
      -- pokud se hledalo pres pretizeby parametr a neco se naslo, tak to neni 
      -- neuspech (hledani pres kontaktni telefon)
      IF p_PaymentAmt IS NOT NULL THEN
        p_result := 0;
        RETURN;        
      END IF;
      
      p_result := -1;
      RETURN;    
    END IF;
    
    -- najit posledni fakturu     
    BEGIN  
      SELECT max (b.bill_nbr) INTO p_InvNbr
      FROM CC.Bill b WHERE b.acct_id = l_acct_id;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN  
        p_InvNbr := null;
    END;
    
    -- udaje o fakture
    IF (NOT (p_InvNbr IS NULL)) THEN
    
      SELECT 
        (b.Due/100), 
        TO_CHAR(bc.debt_date, 'YYYY-MM-DD'),
        (b.pre_balance + b.due + b.recv_charge + b.adjust_charge) / 100
      INTO 
        p_InvAmount, p_InvDueDate, p_ActualBalance
      FROM bill b 
      INNER JOIN billing_cycle bc ON (b.billing_cycle_id=bc.billing_cycle_id)
      WHERE b.bill_nbr = p_InvNbr;   
      
      -- vyjimky potvrzujic pravidlo
      -- pokud se jedna o Euphony, tak zamlcovat dluh
      IF l_acct_id IN (229335) THEN -- Euphony Czech Republic a.s.
        p_ActualBalance := 0;
      END IF;
      
      -- stejne tak pokud se jedna o SIPo zakaznika, tak
      -- zamlcovat dluh
      BEGIN
        SELECT COUNT (*) INTO l_sipo_count
        FROM cc.cust_attr_value cav 
        INNER JOIN cc.acct a ON a.cust_id = cav.cust_id
        WHERE cav.cust_attr_db_id=277 -- EXP_PAYMENT_METHOD
        AND cav.attr_value='SI'       -- SIPO
        AND a.id=l_acct_id;
      
        IF l_sipo_count >0 THEN
          p_ActualBalance := 0;        
        END IF;
      EXCEPTION
        -- sichr
        WHEN others THEN
          NULL;
      END;
      
    ELSE
      p_InvAmount     := NULL;
      p_InvDueDate    := NULL;
      p_ActualBalance := NULL;
    END IF;
    
    -- udaje o aktualnim bill cyklu
    BEGIN

      SELECT 
        TO_CHAR(bc.cycle_begin_date, 'YYYY-MM-DD'),
        TO_CHAR(bc.cycle_end_date, 'YYYY-MM-DD')        
      INTO 
        p_BillCycleDateFrom, p_BillCycleDateTo
      FROM acct a
      INNER JOIN billing_cycle bc ON (bc.billing_cycle_type_id=a.billing_cycle_type_id)
      WHERE acct_nbr=l_acct_id
      AND sysdate BETWEEN bc.cycle_begin_date AND bc.cycle_end_date;
      
    EXCEPTION
      WHEN NO_DATA_FOUND THEN  
        p_BillCycleDateFrom := NULL;
        p_BillCycleDateTo   := NULL;
    END;

    -- udaje o posledni platbe
    IF p_PaymentAmt IS NULL THEN -- pokud jiz sem neslo neco pomoci pretizeni parametru
      BEGIN
        SELECT  
          TO_CHAR(a.created_date, 'YYYY-MM-DD'), -a.charge/100
        INTO
          p_PaymentDate, p_PaymentAmt
        FROM acct_book a where a.acct_book_id  =
        (
           SELECT max (ab.acct_book_id) 
           FROM acct_book ab
           WHERE ab.acct_id=l_acct_id and ab.acct_book_type='P'
        );
    
      EXCEPTION
        WHEN NO_DATA_FOUND THEN      
          p_PaymentDate := NULL;
          p_PaymentAmt  := NULL;
      END;
    END IF;
    -- aktualni bill cyklus
    
    
    p_result := 0;
  END;
  
  -- funkce pro GetMSISDNProfile web service
  procedure GET_SUBS_AND_ACCT (in_MSISDN in cc.subs.acc_nbr%TYPE,
                               p_subs_id out cc.subs.id%TYPE,
                               p_acct_type out varchar2,
                               p_acct_id out cc.acct.id%TYPE,
                               p_rc out integer)
  as

    -- definice exceptions
    ambiguous_acct exception;
    no_acct_found exception;
    
    -- vnitrni parametry procedury
    p_acct_count number(5) := 0;
    p_subs_count number(5) := 0;
    
  begin
    
    p_rc := 0;

    --select konkretniho subs_id pro zadana MSISDN
    begin

      select s.id
      into   p_subs_id
      from   cc.subs s, cc.prod p
      where  s.id = p.id
      and    s.acc_nbr = in_MSISDN
      --neterminovany produkt
      and    p.prod_state != 'B';

      exception
        when no_data_found then
          p_subs_id := null;

    end;

    -- subs id nebylo nalezeno
    -- zacne se hledat subs id podle kontaktniho telefonu
    if p_subs_id is null then

      select count(a.id)
      into   p_acct_count
      from   cc.cust_attr_value cav, cc.acct a
      where  cav.cust_attr_db_id = 333
      and    cav.cust_id = a.cust_id
      and    substr(cav.attr_value,-9) = in_MSISDN;

      if p_acct_count > 1 then
        Raise ambiguous_acct;
      elsif p_acct_count = 0 then
        Raise no_acct_found;
      elsif p_acct_count = 1 then

        --najde hledany account
        begin

          select a.id,
                 case
                   when cav1.attr_value in ('A','B') then
                     'POST'
                   when cav1.attr_value = 'P' then
                     'PREP'
                 end c_type
          into   p_acct_id,
                 p_acct_type
          from   cc.cust_attr_value cav, cc.acct a, cc.cust_attr_value cav1
          where  cav.cust_attr_db_id = 333
          and    cav.cust_id = a.cust_id
          and    substr(cav.attr_value,-9) = in_MSISDN
          and    cav1.cust_id = a.cust_id
          and    cav1.cust_attr_db_id = 816; -- EXP_CUST_CUSTTYPE

          exception
            when no_data_found then
              Raise no_acct_found;

        end;

      end if;

      -- najde vsechny substriptions k vyhledanemu accountu
      select count(s.id)
      into   p_subs_count
      from   cc.subs s, cc.prod p
      where  s.acct_id = p_acct_id
      and    s.id = p.id;

      -- subs ID se pouzije
      if p_subs_count = 1 then

         begin

           select s.id
           into   p_subs_id
           from   cc.subs s, cc.prod p
           where  s.acct_id = p_acct_id
           and    s.id = p.id;

           exception
             when no_data_found then
               p_subs_id := null;

         end;

      end if;

    end if;

    exception
      when ambiguous_acct then
        cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_SUBS_AND_ACCT', 'multiple accounts found for specified MSISDN; MSISDN = ' || in_MSISDN);
        p_rc := 1;
      when no_acct_found then
        cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_SUBS_AND_ACCT', 'no acct found for specified MSISDN; MSISDN = ' || in_MSISDN);
        p_rc := 1;
      when others then
        cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_SUBS_AND_ACCT', sqlerrm || '; MSISDN = ' || in_MSISDN); 
        p_rc := 1;

  end;

  procedure GET_PROFILE (in_MSISDN in cc.subs.acc_nbr%TYPE,
                         in_SUBS_ID in cc.subs.id%TYPE,
                         p_acct_type out varchar2,
                         p_acct_id out cc.acct.id%TYPE,
                         p_msisdn out cc.subs.acc_nbr%TYPE,
                         p_subs_state out cc.prod.prod_state%TYPE,
                         p_tarif_id out cc.subs.price_plan_id%TYPE,
                         p_tarif_desc out cc.price_plan.price_plan_name%TYPE,
                         p_actual_credit out number,
                         p_last_credit_d out date,
                         p_last_credit_a out number,
                         p_last_debit_d out date,
                         p_last_debit_a out number,
                         p_subs_used out integer,
                         p_rc out integer)
  as

    -- definice exceptions
    input_is_null exception;
    nonexistent_subsid exception;
    common_err exception;
    get_actual_credit exception;

    -- vnitrni parametry procedury
    p_subs_id cc.subs.id%TYPE := null;

  begin
    
    -- predpripravena hodnota pro dalsi pouziti
    p_subs_used := 0;
    
    -- inicializace result parametru
    p_rc := 0;

    -----------------------------------
    --kontrola vstupnich parametru
    -----------------------------------

    -- oba vstupni parametry jsou null -> exception
    if in_SUBS_ID is null and in_MSISDN is null then

      Raise input_is_null;

    -- vyplneno je pouze MSISDN
    elsif in_SUBS_ID is null then

      -- ziskani subs ID nebo acct ID
      GET_SUBS_AND_ACCT(in_MSISDN,
                        p_subs_id,
                        p_acct_type,
                        p_acct_id,
                        p_rc);
                        
      if p_rc != 0 then
        Raise common_err;
      end if;

    -- vyplneny jsou oba vstupni parametry nebo jen subs id
    else 

      -- otestuje existenci subs id
      begin
        
        select s.id, s.acc_nbr
        into   p_subs_id, p_msisdn
        from   cc.subs s
        where  s.id = in_SUBS_ID;
      
        exception
          when no_data_found then
            Raise nonexistent_subsid;
      
      end;

    end if;


    -------------------------------------------
    -- dohledani dalsich udaju
    -------------------------------------------
    
    -- mame existujici subs id
    if p_subs_id is not null then
      
      -- najde veci specificke accountu
      if p_acct_id is null then
        
        select s.acct_id,
               case
                 when cav.attr_value in ('A','B') then
                   'POST'
                 when cav.attr_value = 'P' then
                   'PREP'
               end c_type
        into   p_acct_id, p_acct_type
        from   cc.subs s, cc.cust c, cc.cust_attr_value cav, cc.attr a
        where  s.id = p_subs_id 
        and    s.cust_id = c.id
        and    cav.cust_id = c.id
        and    cav.cust_attr_db_id = a.id
        and    a.attr_code = 'EXP_CUST_CUSTTYPE';
        
      end if;
      
      -- najde veci specificke pro suba, stejne pro prepaidy i postpaidy
      select s.acc_nbr,
             p.prod_state,
             s.price_plan_id,
             pp.price_plan_name
      into   p_msisdn, 
             p_subs_state, 
             p_tarif_id, 
             p_tarif_desc
      from   cc.subs s, cc.prod p, cc.price_plan pp
      where  s.id = p.id
      and    s.id = p_subs_id
      and    s.price_plan_id = pp.price_plan_id;
      
      -- zjisteni veci tykajicich se kreditu - pouze u prepaidu
      if p_acct_type = 'PREP' then
        
        -- aktualni kredit - parametr hlavniho produktu
        begin
          
          select psb.amount/100
          into   p_actual_credit
          from   gemini.pp_subs_ballance psb
          where  psb.subs_id = p_subs_id
          and    psb.acct_id = p_acct_id;
          
          exception
            when no_data_found then
              p_actual_credit := 0;
        
        end;
        
        -- posledni amount a date credit operace
        begin
            
          select tt.trans_date, tt.credit_amount/100
          into   p_last_credit_d, p_last_credit_a
          from   
                 (
                  select   ptb.trans_date, 
                           ptb.credit_amount,
                           max(ptb.credit_amount) over (partition by ptb.subs_id, ptb.acct_id) m_credit_amount,
                           max(ptb.trans_date) over (partition by ptb.subs_id, ptb.acct_id) m_trans_date
                  from     gemini.pp_transation_book ptb
                  where    ptb.subs_id = p_subs_id 
                  and      ptb.acct_id = p_acct_id
                  and      ptb.trans_type = 'C'
                 ) tt
          where  tt.trans_date = tt.m_trans_date    
          and    tt.credit_amount = tt.m_credit_amount
          and    rownum = 1;
            
          exception
            when no_data_found then
              p_last_credit_d := null;
              p_last_credit_a := null;
          
        end;
        
        -- posledni amount a date debit operace
        begin
          
          select tt.trans_date, tt.debit_amount/100
          into   p_last_debit_d, p_last_debit_a
          from   
                 (
                  select   ptb.trans_date, 
                           ptb.debit_amount,
                           max(ptb.debit_amount) over (partition by ptb.subs_id, ptb.acct_id) m_debit_amount,
                           max(ptb.trans_date) over (partition by ptb.subs_id, ptb.acct_id) m_trans_date
                  from     gemini.pp_transation_book ptb
                  where    ptb.subs_id = p_subs_id 
                  and      ptb.acct_id = p_acct_id
                  and      ptb.trans_type = 'D'
                 ) tt
          where  tt.trans_date = tt.m_trans_date
          and    tt.debit_amount = tt.m_debit_amount
          and    rownum = 1;
            
          exception
            when no_data_found then
              p_last_debit_d := null;
              p_last_debit_a := null;
              
        end;
        
      end if;
            
    end if;


    -- neni subs id ale je acct id
    -- vraci se pouze acct_id a acct_type


    exception
      when input_is_null then
        cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_PROFILE', 'both input parameters are null');
        p_rc := 1;
      when nonexistent_subsid then
        cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_PROFILE','specified subs id does not exist; SubsId = ' || in_SUBS_ID || ', MSISDN = ' || in_MSISDN);
        p_rc := 1;
      when common_err then
        p_rc := 1;
      when others then
        cc_comon.log_event(sysdate, 'WS_FUNCTIONS.GET_PROFILE', sqlerrm || '; SubsId = ' || in_SUBS_ID || ', MSISDN = ' || in_MSISDN);
        p_rc := 1;

  end;
  
  PROCEDURE get_account_debt (p_acct_id NUMBER, p_due OUT NUMBER, p_rc OUT NUMBER, p_msg OUT VARCHAR2)
  IS
  BEGIN
    -- nova verze zapocitavajic SIPO korekce
    -- p_due := collection.cc_collection_common.get_due_amount(p_acct_id);
    p_due := collection.cp_main.get_actual_due (p_acct_id) / 100;    
    
    -- preplatek
    IF NOT (p_due IS NULL) THEN
      IF p_due < 0 THEN
        p_due := 0;
      END IF;
    END IF;
    
    p_rc := 0;
    p_msg := 0;
--    p_due := 42;
  EXCEPTION
    WHEN others THEN
      p_due := NULL;
      p_rc := 0;
      p_msg := sqlerrm;      
  END;
    

end WS_FUNCTIONS_DEV;
/
