Startup Instructions
--------------------
./Reload_Project.sh "FactorioApi" "--port=6001" "git@github.com:roman015/ServerAPIs.git"

To Setup Factorio-Terraform
--------------------
git clone git@github.com:abrahamvarricatt/factorio-terraform.git

To Start Factorio server manually
--------------------
terraform apply -auto-approve

To Stop Factorio server manually
--------------------
terraform destroy -auto-approve

To Get IP Address of Running Server
--------------------
terraform output public_ip

To Use SSH 
--------------------
ssh -o UserKnownHostsFile=/dev/null -o StrictHostKeyChecking=no -i ~/data/factorio-terraform/aws_ssh_key ubuntu@54.254.235.210

To Copy The Save File Over
--------------------
scp -o UserKnownHostsFile=/dev/null -o StrictHostKeyChecking=no -i ~/data/factorio-terraform/aws_ssh_key ubuntu@13.251.26.41:/opt/factorio/saves/server-save.zip ~/data/factorio-terraform/server-save.zip