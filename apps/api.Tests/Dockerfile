FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src
COPY ["api.Tests/api.Tests.csproj", "api.Tests/"]
COPY ["api/api.csproj", "api/"]
RUN dotnet restore "api.Tests/api.Tests.csproj"

COPY . .
WORKDIR "/src/api.Tests"
RUN dotnet build "api.Tests.csproj" -c Release -o /app/build

FROM build AS test
WORKDIR /src/api.Tests
ENTRYPOINT ["dotnet", "test", "--logger:trx", "--results-directory:/testresults"]
