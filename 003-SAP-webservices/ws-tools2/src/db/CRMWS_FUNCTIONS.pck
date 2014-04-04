create or replace package CRMWS_FUNCTIONS is

  -- ==================================================================
  --   rozhrani pro SAP CRM (faza 3 nebo tak neco)
  --   udaje o zakaznikovych fakturacha a o historii produktu
  --
  --   25.9.2009 lojza
  -- ==================================================================
  
  -- 
  --  zakaznikovy faktury
  -- 
  PROCEDURE get_customer_invoces (  
     p_acct_id NUMBER, p_crm_id VARCHAR2, p_xml_txt OUT CLOB, p_debt OUT NUMBER,
     p_rc OUT NUMBER, p_msg OUT VARCHAR2);


  -- 
  -- historie produktu
  --
  PROCEDURE get_prod_history ( 
     p_prod_id NUMBER, 
     p_curr_state OUT sys_refcursor, p_curr_his OUT sys_refcursor,
     p_rc OUT NUMBER, p_msg OUT VARCHAR2);


end CRMWS_FUNCTIONS;
/
create or replace package body CRMWS_FUNCTIONS is


  -- forwar deklarace loklani funkce
  PROCEDURE process_invoces (p_acct_id NUMBER, p_xml OUT CLOB, p_actual_debt OUT NUMBER);

  -- faktury zakaznika
  PROCEDURE get_customer_invoces (  
     p_acct_id NUMBER, p_crm_id VARCHAR2, p_xml_txt OUT CLOB, p_debt OUT NUMBER,
     p_rc OUT NUMBER, p_msg OUT VARCHAR2)
  IS
    l_acct_id NUMBER;
  BEGIN
  
    p_rc := NULL;
    p_msg := NULL;  
  
    l_acct_id := NULL;
    
    -- mame crm id, prevedeme ho na acct_id
    IF (NOT (p_crm_id IS NULL)) THEN
      
      -- pohleda account id
      BEGIN
        SELECT a.id INTO l_acct_id
        FROM  cc.acct a 
        INNER JOIN cc.cust_attr_value cav ON
          (cav.cust_id=a.cust_id AND cav.cust_attr_db_id=472 /* 472 = CRM ID*/ ) 
        WHERE cav.attr_value = p_crm_id;        
      EXCEPTION
        WHEN NO_DATA_FOUND THEN
          p_rc := -1;
          p_msg := 'invalid crm_id';
      END;      
    ELSE
      l_acct_id := p_acct_id;
    END IF;
    
    IF ( l_acct_id IS NULL ) THEN
      p_rc := -2;
      p_msg := 'invalid crm_id or acct_id';
      RETURN;
    END IF;
    
    -- najdeme faktury, ktere k tomu patri
    process_invoces (l_acct_id, p_xml_txt, p_debt);
    p_rc  := 0;
    p_msg := 0;
  END;
  

  -- historie produktu
  PROCEDURE get_prod_history ( 
     p_prod_id NUMBER, 
     p_curr_state OUT sys_refcursor, p_curr_his OUT sys_refcursor,
     p_rc OUT NUMBER, p_msg OUT VARCHAR2) IS
  BEGIN
  
    -- historie stavu produktu
    OPEN p_curr_state FOR    
    select 
      ps.prod_state_name, 
      ph.prod_state, 
      to_char (ph.eff_date, 'YYYY-MM-DD hh24:mi:ss') as date_from, 
      to_char (ph.exp_date, 'YYYY-MM-DD hh24:mi:ss') as date_to
    from cc.prod_his ph
    inner join cc.prod_state ps on (ps.prod_state=ph.prod_state)
    where ph.id = p_prod_id
    order by ph.eff_date;

    -- historie atributu produktu
    OPEN p_curr_his FOR
    

    SELECT 
      a.id,a.attr_name, NVL (av.value_mark, pah.value) AS text_value,
      to_char (pah.eff_date, 'YYYY-MM-DD hh24:mi:ss') as date_from, 
      to_char (pah.exp_date, 'YYYY-MM-DD hh24:mi:ss') as date_to
    FROM 
      cc.PROD_ATTR_VALUE_HIS pah
      INNER JOIN cc.attr a on (a.id =pah.attr_id)
      LEFT JOIN cc.attr_value av on (pah.attr_id = av.base_attr_id and pah.value=av.value)
    WHERE 
      a.csr_visible = 'Y' AND
      pah.prod_id = p_prod_id
    ORDER BY 
      pah.eff_date, a.id;
        
    p_rc := 0;
    p_msg := null;
  END;
  
  --
  -- slozita funkce ktra napocita, jake ma zakaznik faktury a jak jsou zaplacene
  -- ci vydobriposovane
  -- 
  PROCEDURE process_invoces (p_acct_id NUMBER, p_xml OUT CLOB, p_actual_debt OUT NUMBER)
  IS
  
    -- obycejne loklani promenne
    l_clob CLOB;
    unalloc_pay NUMBER;
    unalloc_adj NUMBER;
    l_last_bill_idx NUMBER;
    l_idx NUMBER;   
    l_bill_nbr cc.bill.bill_nbr%TYPE;
    l_bill_date VARCHAR2(15);
    l_debt_date VARCHAR2(15);
    l_amount NUMBER;
    l_total_debt NUMBER;
    c_lf VARCHAR2(2);
    
    -- polue struktur
    TYPE bill_rec IS RECORD 
    (
      bill_id  cc.acct_book.bill_id%TYPE,
      due NUMBER,
      recieved NUMBER,
      adjusted NUMBER,
      debt NUMBER
    );
   
    TYPE bill_list IS VARRAY(9000) OF bill_rec;
    bills  BILL_LIST;


    -- cusrosr so smycky
    -- http://www.fast-track.cc/t_easyoracle_pl_sql_cursor_for_loop.htm
    CURSOR book_cur (v_acct_id IN NUMBER) IS
      SELECT ab.acct_book_type, ab.charge, ab.bill_id, b.bill_nbr
      FROM cc.acct_book ab
      LEFT JOIN cc.bill b ON (b.bill_id=ab.bill_id)
      WHERE ab.acct_id=v_acct_id
      ORDER BY ab.acct_book_id;  
  BEGIN
    c_lf := chr(10);  -- enter (LF)

    unalloc_pay := 0;
    unalloc_adj  := 0;
    l_last_bill_idx := NULL;
    bills := BILL_LIST();
  
    -- projet vsechny zaznamy z acct_book
    FOR book_rec IN book_cur (p_acct_id)
    LOOP
  
      -- faktura
      IF ( book_rec.acct_book_type = 'I' ) THEN
        bills.extend(1);  
      
        -- zakladni veci o fakture o fakture
        bills(bills.last).bill_id := book_rec.bill_id;
        bills(bills.last).due := book_rec.charge;
      
        -- inicializace
        bills(bills.last).recieved := 0;   
        bills(bills.last).adjusted := 0;
        bills(bills.last).debt := book_rec.charge;
      
      END IF;
    
      -- platba
      IF ( book_rec.acct_book_type = 'P' ) THEN
        l_amount := (- book_rec.charge);
        -- pokud to patri na posledni fakturu a je na ni misto, tak to tam dat
        IF ( (bills.count > 0) AND (bills(bills.last).bill_id = book_rec.bill_id)) THEN
          l_idx := bills.last;
        
          -- pokud se to ta cele vejde (viz. napr ACCT_ID 20687)
          IF ( bills(l_idx).debt >= l_amount ) THEN          
            -- zvednout adjustent, snizit dluh a snulovat amount
            bills(l_idx).recieved := bills(l_idx).recieved + l_amount;
            bills(l_idx).debt     := bills(l_idx).debt     - l_amount;
            l_amount := 0;
          
          END IF;        
        END IF;
      
        unalloc_pay := unalloc_pay + l_amount;
      END IF;
    
      -- dobropis
      IF ( book_rec.acct_book_type = 'A' ) THEN
        l_amount := (- book_rec.charge);

        -- pokud je na fakture, kam to patri misto, tak to tam dat
        -- adjustovat se mohou i stare faktury      
        FOR l_idx IN bills.first .. bills.last LOOP
          IF (bills(l_idx).bill_id = book_rec.bill_id) THEN
        
            -- pokud se to ta cele vejde (viz. napr ACCT_ID 20687)
            IF ( bills(l_idx).debt >= l_amount ) THEN
          
              -- zvednout adjustent, snizit dluh a snulovat amount
              bills(l_idx).adjusted := bills(l_idx).adjusted + l_amount;
              bills(l_idx).debt     := bills(l_idx).debt     - l_amount;
              l_amount := 0;
            
            END IF;
          END IF;
        END LOOP;

        -- kdyby nahodou neco zbylo, tak to dat do globalniho citace
        unalloc_adj := unalloc_adj + l_amount;      
      END IF;

    END LOOP; -- konec nacitani acct_book

    -- zacne se od prvni faktury, pokud ji mame  
    IF (bills.count > 0) THEN
      l_last_bill_idx := bills.first;
    ELSE
      -- sichr, neni zadna faktura, neni co rozmistovat
      l_last_bill_idx := NULL;
    END IF;

    -- projet faktury a rozdelit na ne neumistene platby a adjustmenty  
    WHILE ((unalloc_adj >0) OR (unalloc_pay>0) AND  ( NOT (l_last_bill_idx is null)))
    LOOP
      -- mame dluznou dakturu
      IF (bills(l_last_bill_idx).debt > 0) THEN
    
        -- adjustent 
        IF (unalloc_adj > 0) THEN
      
          l_amount := unalloc_adj;
          IF (bills(l_last_bill_idx).debt < l_amount) THEN
            l_amount := bills(l_last_bill_idx).debt;
          END IF;
          
          unalloc_adj := unalloc_adj - l_amount;
          bills(l_last_bill_idx).debt := bills(l_last_bill_idx).debt - l_amount;
          bills(l_last_bill_idx).adjusted := bills(l_last_bill_idx).adjusted + l_amount;
        END IF;
      
        -- platba
        IF (unalloc_pay > 0) THEN
      
          l_amount := unalloc_pay;
          IF (bills(l_last_bill_idx).debt < l_amount) THEN
            l_amount := bills(l_last_bill_idx).debt;
          END IF;
          
          unalloc_pay := unalloc_pay - l_amount;
          bills(l_last_bill_idx).debt := bills(l_last_bill_idx).debt - l_amount;
          bills(l_last_bill_idx).recieved := bills(l_last_bill_idx).recieved + l_amount;
        END IF;           
      END IF;
    
      -- pokusit se najit dalsi dluznou fakturu, pokud je to nutne
      IF (bills(l_last_bill_idx).debt = 0) THEN    
    
        -- pokud muzeme nekam popojet, tak popojedem,
        -- jsme-li na posledni fakture, tak skoncit (preplatek)      
        IF (l_last_bill_idx = bills.last) THEN
           EXIT; -- ukoncit cyklus
        END IF;
      
        l_last_bill_idx := l_last_bill_idx + 1;
      END IF;
    
    END LOOP; -- konec projizdeni faktur
  
    -- sestaveni vystupu a zaverecnych statistik
    l_total_debt := - (unalloc_pay + unalloc_adj);
    l_clob := '<bills>' || C_LF;
    FOR l_idx IN bills.first .. bills.last LOOP
  
      -- nascitat celkovy dluh, pokud je
      IF (bills(l_idx).debt != 0) THEN
        l_total_debt := l_total_debt + bills(l_idx).debt;
      END IF;
  
      -- pohledat dalsi udaje o fakutre (spatnost, cislo ...)
      SELECT b.bill_nbr, TO_CHAR(bc.cycle_end_date, 'YYYY-MM-DD'), TO_CHAR(bc.debt_date, 'YYYY-MM-DD') 
      INTO l_bill_nbr, l_bill_date, l_debt_date
      FROM cc.bill b
      INNER JOIN cc.billing_cycle bc ON bc.billing_cycle_id=b.billing_cycle_id
      WHERE b.bill_id = bills(l_idx).bill_id;
    
      -- sestavit XML
      l_clob := l_clob || 
        ' <bill>'        || C_LF ||
        '  <nbr>'        || l_bill_nbr            || '</nbr>'       || C_LF ||
        '  <bill_date>'  || l_bill_date           || '</bill_date>' || C_LF ||
        '  <debt_date>'  || l_debt_date           || '</debt_date>' || C_LF ||
        '  <due>'        || bills(l_idx).due      || '</due>'       || C_LF ||
        '  <debt>'       || bills(l_idx).debt     || '</debt>'      || C_LF ||
        '  <recieved>'   || bills(l_idx).recieved || '</recieved>'  || C_LF ||
        '  <adjusted>'   || bills(l_idx).adjusted || '</adjusted>'  || C_LF ||
        ' </bill>' || C_LF;

    END LOOP; -- cyklus na tvorbu vystupu
    l_clob := l_clob || '</bills>';
    
    p_xml := l_clob;
    p_actual_debt := l_total_debt;   
  END;  

end CRMWS_FUNCTIONS;
/
