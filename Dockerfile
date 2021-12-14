FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /app
COPY *.sln ./
COPY *.csproj ./Battlesnake.AI/
COPY *.csproj ./Battlesnake.Algorithm/
COPY *.csproj ./Battlesnake.API/
COPY *.csproj ./Battlesnake.DTOModel/
COPY *.csproj ./Battlesnake.Enum/
COPY *.csproj ./Battlesnake.Model/
COPY *.csproj ./Battlesnake.Train/
COPY *.csproj ./Battlesnake.Utility/

#RUN dotnet restore
COPY . ./
#WORKDIR /app/Battlesnake.AI
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.Algorithm
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.API
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.DTOModel
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.Enum
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.Model
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.Train
#RUN donet build -c Release -o /app
#
#WORKDIR /app/Battlesnake.Utility
#RUN donet build -c Release -o /app

WORKDIR /app
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "webapp-cloudrun.dll"]