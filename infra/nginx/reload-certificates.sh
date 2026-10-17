#!/bin/sh
# DIST-04: certbot renews the downloads certificate in its own container; nginx picks it up on a reload.
(while sleep 21600; do nginx -s reload; done) &
