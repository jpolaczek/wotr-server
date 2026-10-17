#!/bin/sh
# DIST-04: the downloads certificate by DNS-01 with a Cloudflare token limited to editing the zone's DNS, then a
# renewal check twice a day. No e-mail account: renewal is automatic and expiry warnings would go nowhere useful.
set -eu
umask 077
credentials=/tmp/cloudflare.ini
printf 'dns_cloudflare_api_token = %s\n' "$(cat /run/secrets/cloudflare_token)" > "$credentials"
staging=""
[ -n "${WOTR_CERTBOT_STAGING:-}" ] && staging="--test-cert"
certbot certonly --non-interactive --agree-tos --register-unsafely-without-email --keep-until-expiring \
  --dns-cloudflare --dns-cloudflare-credentials "$credentials" --dns-cloudflare-propagation-seconds 30 \
  -d "$WOTR_DOWNLOADS_HOST" $staging
while :; do
  sleep 43200
  certbot renew --non-interactive --dns-cloudflare-credentials "$credentials" || echo "certbot renew failed; retrying in 12 h"
done
