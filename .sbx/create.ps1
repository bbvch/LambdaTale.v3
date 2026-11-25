param (
  [string]
  $Name = 'lambdatale'
)
sbx env create --name $Name
git remote add sbx "$Name.sbx:/home/agent/workspace"
git push sbx HEAD:host
sbx exec $Name git co host -b work
