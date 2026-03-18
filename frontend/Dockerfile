FROM node:20 AS development

WORKDIR /usr/src/app

COPY package*.json ./

RUN npm i

COPY . .

RUN npm run build

FROM nginx:alpine

COPY .nginx/nginx.conf /etc/nginx/conf.d/default.conf
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*

COPY --from=development /usr/src/app/dist /usr/share/nginx/html/

ENTRYPOINT ["nginx","-g","daemon off;"]