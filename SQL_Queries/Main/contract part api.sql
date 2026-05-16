select ERR_MSG,* from EGS_MASS_Input order by recid desc

select inp.ERR_MSG,* from EGS_MASS_Inlog log
join EGS_MASS_Input inp
on inp.LOGNUM = log.LOGNUM
where log.LOGNUM = '711'