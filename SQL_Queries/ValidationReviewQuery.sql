-- Customer created 
select CUSTGROUP,ACCOUNTNUM,* from CUSTTABLE 
where CUSTGROUP = 'TST_CustGr'
order by recid desc 

-- Installation created 
select INSTNUM,INSTREADINGMETHODNUM,* from EXU_INSTALLATION 
where INSTREADINGMETHODNUM like  'TST%'

--where INSTOWNER = '917585'
order by RECID desc

/*
	-- Customer
	917585
	917586

	Contract
	9367878
*/


select eu.INSTNUM,uinst.DELIVERYCATEGORY from EXU_INSTALLATION eu
join EXU_UTILITYINSTALLATION uinst
on uinst.INSTNUM = eu.INSTNUM
where eu.INSTREADINGMETHODNUM like  'TST%'
order by eu.RECID desc

select ct.CONTRACTNUM,ct.ATTACHMENTNUM, cust.ACCOUNTNUM, * from EXU_CONTRACTTABLE ct
join EXU_INSTALLATION inst
on inst.INSTNUM= ct.ATTACHMENTNUM
join custtable cust
on cust.ACCOUNTNUM = ct.CUSTACCOUNT
where inst.INSTREADINGMETHODNUM like  'TST%'
and cust.CUSTGROUP like  'TST%'

--==========================================================================================
-- General queries 

select ERR_MSG,* from EGS_MASS_Input
where C006 = '9367906'

--delete from EGS_MASS_Input where C006 = '9367906'

select * from SysAADClientTable

select top 1 name,dv.DISPLAYVALUE

--,*
from DIMENSIONATTRIBUTE da
join DIMENSIONATTRIBUTEVALUE dv
on da.RECID = dv.DIMENSIONATTRIBUTE
where name not like '%System%'
order by da.NAME

select * from XAPI_APIConfigurations

select * from XAPI_APILogTable

update XAPI_APILogTable
set archived  = 0

delete from XAPI_APILogTable



/*
	0	CustAccount	917600		NULL	oab	5637144576	5637149826	1	0x00000000727A4E37	{"$id":"1","ErrorCode":"Ok","ResponseCode":"","Status":"","Message":"917600"}
	1	InstNum	19739		NULL	oab	5637144576	5637149827	1	0x00000000727A4E55	{"$id":"1","InstallationNum":"19739","ErrorCode":"","ResponseCode":"","Status":"Ok","Message":""}
	2	ContractNum	9367851		NULL	oab	5637144576	5637149828	1	0x00000000727A4E5A	{"$id":"1","ErrorCode":"","ResponseCode":"","Status":"Ok","Message":"9367851"}

	*/