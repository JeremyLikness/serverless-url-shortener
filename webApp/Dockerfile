from busybox:latest
run mkdir /www 
copy index.html /www
copy index.js /www
expose 80
cmd ["httpd", "-f", "-p", "80", "-h", "/www"]