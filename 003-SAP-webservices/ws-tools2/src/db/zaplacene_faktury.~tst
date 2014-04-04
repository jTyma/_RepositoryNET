PL/SQL Developer Test script 3.0
218
--
-- pokusny script, ktery byl nasledne prenesen do funkce CRMWS_FUNCTIONS.process_invoces
--

declare 
  p_acct_id integer;
  l_clob CLOB;
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
unalloc_pay NUMBER;
unalloc_adj NUMBER;
l_last_bill_idx NUMBER;
l_idx NUMBER;

l_bill_nbr cc.bill.bill_nbr%TYPE;
l_bill_date VARCHAR2(15);
l_debt_date VARCHAR2(15);

  -- http://www.fast-track.cc/t_easyoracle_pl_sql_cursor_for_loop.htm
  CURSOR book_cur (v_acct_id IN NUMBER) IS
    SELECT ab.acct_book_type, ab.charge, ab.bill_id, b.bill_nbr
    FROM cc.acct_book ab
    LEFT JOIN cc.bill b ON (b.bill_id=ab.bill_id)
    WHERE ab.acct_id=v_acct_id
    ORDER BY ab.acct_book_id;

  l_amount NUMBER;
  l_total_debt NUMBER;
  c_lf VARCHAR2(2);
  
begin
  p_acct_id := :acct_id;
  c_lf := chr(10);

  unalloc_pay := 0;
  unalloc_adj  := 0;
  l_last_bill_idx := NULL;
  bills := BILL_LIST();
  
  FOR book_rec IN book_cur (p_acct_id)
  LOOP
  
    IF ( book_rec.acct_book_type = 'I' ) THEN
      bills.extend(1);  
      
      -- zaklady o fakture
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

      -- kdyby nahodou neco zbylo (coz by nemelo), tak to dat do globalniho citace
      unalloc_adj := unalloc_adj + l_amount;      
    END IF;

  END LOOP; 
  
  -- protoze platby a adjustemnty muzou byt s opacnym znamenkem, tak je nejprve vyscitame
  -- a pak je rozdelime na faktury
  IF (bills.count > 0) THEN
    l_last_bill_idx := bills.first;
  ELSE
    -- sichr, neni zadna faktura, neni co rozmistovat
    l_last_bill_idx := NULL;
  END IF;
  
  
  
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
    
    -- pokusit se najit dalsi dluznou fakturu
    IF (bills(l_last_bill_idx).debt = 0) THEN    
    
      -- pokud muzeme nekam popojet, tak popojedem,
      -- jsme-li na posledni fakture, tak skoncit (preplatek)      
      IF (l_last_bill_idx = bills.last) THEN
         EXIT;
      END IF;
      
      l_last_bill_idx := l_last_bill_idx + 1;
    END IF;
    
  END LOOP;
  
  -- sestaveni vystupu a zaverecnych statistik
  l_total_debt := - (unalloc_pay + unalloc_adj);
  l_clob := '<bills>' || C_LF;
  FOR l_idx IN bills.first .. bills.last LOOP
  
    -- nacist dluh, pokud je
    IF (bills(l_idx).debt != 0) THEN
      l_total_debt := l_total_debt + bills(l_idx).debt;
    END IF;
  
    -- pohledat k fakutre udaje
    SELECT b.bill_nbr, TO_CHAR(bc.cycle_end_date, 'YYYY-MM-DD'), TO_CHAR(bc.debt_date, 'YYYY-MM-DD') 
    INTO l_bill_nbr, l_bill_date, l_debt_date
    FROM cc.bill b
    INNER JOIN cc.billing_cycle bc ON bc.billing_cycle_id=b.billing_cycle_id
    WHERE b.bill_id = bills(l_idx).bill_id;
    

  
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

/*  
    dbms_output.put_line ( 
      bills(l_idx).due || ' ' || 
      bills(l_idx).debt || ' ' || 
      bills(l_idx).recieved || ' ' || 
      bills(l_idx).adjusted);
*/      
  END LOOP;
  l_clob := l_clob || '</bills>';
  
  
  --dbms_output.put_line ( 'preplatek: '  || unalloc_pay || ' ' || unalloc_adj);
--  dbms_output.put_line ('celkovy dluh:' || l_total_debt);

  dbms_output.put_line (l_clob);
  :c := l_clob;
  
end;
2
acct_id
1
20687
3
c
1
<CLOB>
112
1
bills.count
