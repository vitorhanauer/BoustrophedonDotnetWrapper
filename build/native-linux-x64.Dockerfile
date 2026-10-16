FROM debian:12-slim AS build

RUN apt-get update \
    && apt-get install --no-install-recommends -y cmake gcc libc6-dev make \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
ADD core.tar ./
RUN cmake -S . -B /tmp/cmake-build -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=OFF \
    && cmake --build /tmp/cmake-build --parallel 2 --target boustrophedon_shared \
    && mkdir -p /out \
    && cp -L /tmp/cmake-build/libboustrophedon.so /out/libboustrophedon.so

FROM debian:12-slim
COPY --from=build /out/libboustrophedon.so /out/libboustrophedon.so
CMD ["/bin/true"]
