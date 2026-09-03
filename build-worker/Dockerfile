# ─── Stage 1: Build the .NET worker ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/FlutterPlatform.BuildWorker/FlutterPlatform.BuildWorker.csproj .
RUN dotnet restore
COPY src/FlutterPlatform.BuildWorker/ .
RUN dotnet publish -c Release -o /app/publish

# ─── Stage 2: Runtime with Flutter + Android SDK ─────────────────────────────
FROM ubuntu:22.04 AS runtime

ENV DEBIAN_FRONTEND=noninteractive
ENV FLUTTER_VERSION=3.27.1
ENV ANDROID_COMPILE_SDK=34
ENV ANDROID_BUILD_TOOLS=34.0.0
ENV ANDROID_SDK_ROOT=/opt/android-sdk
ENV FLUTTER_HOME=/flutter
ENV PATH="${FLUTTER_HOME}/bin:${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${ANDROID_SDK_ROOT}/build-tools/${ANDROID_BUILD_TOOLS}:${PATH}"

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    git \
    unzip \
    xz-utils \
    zip \
    libglu1-mesa \
    openjdk-17-jdk \
    wget \
    ca-certificates \
    gnupg \
    && rm -rf /var/lib/apt/lists/*

# Install .NET runtime
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --runtime dotnet --install-dir /usr/share/dotnet
ENV PATH="/usr/share/dotnet:${PATH}"

# Install Flutter SDK
RUN git clone --depth 1 --branch ${FLUTTER_VERSION} https://github.com/flutter/flutter.git ${FLUTTER_HOME} \
    && flutter precache --android \
    && flutter config --no-analytics

# Install Android SDK command-line tools
RUN mkdir -p ${ANDROID_SDK_ROOT}/cmdline-tools && \
    wget -q https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O /tmp/cmdtools.zip && \
    unzip -q /tmp/cmdtools.zip -d /tmp/cmdtools && \
    mv /tmp/cmdtools/cmdline-tools ${ANDROID_SDK_ROOT}/cmdline-tools/latest && \
    rm -rf /tmp/cmdtools /tmp/cmdtools.zip

# Accept licenses and install required SDK components
RUN yes | sdkmanager --licenses && \
    sdkmanager "platform-tools" "platforms;android-${ANDROID_COMPILE_SDK}" "build-tools;${ANDROID_BUILD_TOOLS}"

# Pre-warm Flutter Android toolchain
RUN flutter doctor --android-licenses || true && flutter doctor || true

WORKDIR /app
COPY --from=build /app/publish .

# Create workspace directory
RUN mkdir -p /tmp/flutter-builds /secrets

ENTRYPOINT ["dotnet", "FlutterPlatform.BuildWorker.dll"]
