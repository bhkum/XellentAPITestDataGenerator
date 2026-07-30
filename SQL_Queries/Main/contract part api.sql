--select ERR_MSG,* from EGS_MASS_Input order by recid desc

select inp.ERR_MSG,* from EGS_MASS_Inlog log
join EGS_MASS_Input inp
on inp.LOGNUM = log.LOGNUM
where log.LOGNUM = '712'

select * from INVENTTABLE where ITEMID like 'Ts%'

select PRODUCTNUM,* from EXU_ProductTable where PRODUCTNUM like 'Ts%'