#!/usr/bin/perl

while (<>)
{
	chomp;
	($nbr, $sap_id, $p1, $p2) = split (/\t/);

	if (length($sap_id) eq 8 )
	{
		$sap_id = '00' . $sap_id;
	}

	#print "  set_values ('$nbr', '$sap_id', '$p1$p2');\n";
	print "$nbr\t$sap_id\t$p1$p2\n";
}
