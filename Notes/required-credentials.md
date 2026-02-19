# Credentials Required for Development and Release

## Users and Account Definitions
- Google Cloud Project (GCS_PROJECT__ID): morrisdev-203721
- Google Cloud Service Account (GCS_SERVICE_ACCOUNT): dcelectric@morrisdev-203721.iam.gserviceaccount.com
- 
## Deployment
- You need a JSON Service Account file in your secrets folders.  
	- eg 
```json
	{
  "type": "service_account",
  "project_id": "{GCS_PROJECT__ID}",
  "private_key_id": "--- PRIVATE KEY ID---",
  "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvAIBAD_____PRIVATE_KEY_HERE___1N3/C8Rt3w==\n-----END PRIVATE KEY-----\n",
  "client_email": "{GCS_SERVICE_ACCOUNT}",
  "client_id": "11760-- service account id --61640",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/dcelectric%40morrisdev-203721.iam.gserviceaccount.com",
  "universe_domain": "googleapis.com"
}
``` 

The service account needs Cloud Run deploy permissions. Run these commands:

### Granting Permissions
You need a set of permission to run the system and also deploy it.  The service account can be tested for these permissions by running this:
```
gcloud projects get-iam-policy morrisdev-203721 \
  --flatten="bindings[].members" \
  --filter="bindings.members:GCS_SERVICE_ACCOUNT" \
  --format="table(bindings.role)"
```

You should get these results back:
- roles/artifactregistry.writer
- roles/cloudbuild.builds.editor
- roles/iam.serviceAccountUser
- roles/run.admin
- roles/serviceusage.serviceUsageConsumer
- roles/storage.admin

These can be added individually or with a script like this BASH script:
```bash
SA="serviceAccount:GCS_SERVICE_ACCOUNT"
PROJECT="{GCS_PROJECT__ID}"

for role in \
  roles/artifactregistry.writer \
  roles/cloudbuild.builds.editor \
  roles/iam.serviceAccountUser \
  roles/run.admin \
  roles/serviceusage.serviceUsageConsumer \
  roles/storage.admin; do
    gcloud projects add-iam-policy-binding $PROJECT \
      --member="$SA" \
      --role="$role"
done
```
 