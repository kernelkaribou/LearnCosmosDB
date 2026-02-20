FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["shared/LearnCosmosDB.Shared.csproj", "shared/"]
COPY ["web/LearnCosmosDB.Web.csproj", "web/"]
RUN dotnet restore "web/LearnCosmosDB.Web.csproj"
COPY shared/ shared/
COPY web/ web/
WORKDIR /src/web
RUN dotnet publish -c Release -o /app/publish

FROM nginx:alpine AS final
RUN addgroup -g 101 -S nginx 2>/dev/null || true && \
    adduser -S -D -H -u 101 -h /var/cache/nginx -s /sbin/nologin -G nginx nginx 2>/dev/null || true && \
    chown -R nginx:nginx /var/cache/nginx /var/log/nginx /etc/nginx/conf.d && \
    touch /var/run/nginx.pid && chown nginx:nginx /var/run/nginx.pid
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY web/nginx.conf /etc/nginx/conf.d/default.conf
USER nginx
EXPOSE 80
