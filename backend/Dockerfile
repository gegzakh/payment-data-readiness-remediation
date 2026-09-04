# One image definition for every backend service; PROJECT selects which one is published.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY . .
# The apphost is symlinked to a fixed name so the runtime stage needs no shell: a shell entrypoint would
# drop configuration environment variables whose names contain dashes (YARP cluster ids).
RUN dotnet publish "${PROJECT}" -c Release -o /app \
    && ln -s "/app/$(basename "${PROJECT}")" /app/service

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["/app/service"]
