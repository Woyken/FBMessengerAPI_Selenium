FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .
COPY --from=build-env /app/bin/Release/netcoreapp2.0/chromedriver .
ENTRYPOINT ["dotnet", "MessengerAPI.dll"]

RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libnss3 \
    libx11-6 \
    wget

RUN wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
RUN dpkg -i google-chrome-stable_current_amd64.deb; apt-get -fy install