SAP potøebuje rùzné Web services, kterými se
doptává na stavy produktù, faktura a pod. Je to rùzné.

Zde jsou soustøedìny tyto Web services.

Pùvodních nìkolik webservices vzniklo ještì ve Visual Studio 2003
v .NET frameworku 1.4. To proto, že mìli bìžet na starém serveru
od Èíòana (staré Windows) co si tehdy nikdo neodvážil aktualizovat.

Tydle jsou soustøedìny v adresáøi 

   ws-tools

V adresáøi 

   doc

Je dokumentace k jednotlivým službám.

Zde je tøeba zmínit soubor 

  doc/getAcctInfo-dodatecne-hodnoty.txt

kde je vysvìtlený jistý "dirty hack", kterak využívat webservice

  getAcctInfo.txt

k jiným vìcem než na co byly urèeny a to proto že úpravy volacího
programu (plechová huba od DAVOSu) a cesty k nìmu (SAP XI) by byla
zdlouhavá a nároèná.


Postupným vývojem vznikly nové webservices co již vznikly v adresáøi

  ws-tools2

Ty už jsou ve Visual Studiu 2005 a zde doporuèuji pøidávat nové.

Jedná se o smìs k rùzným úlohám, dokumendace je opìt v adresáøi

  doc


