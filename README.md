# BoustrophedonDotnetWrapper

Wrapper .NET 10 para gerar waypoints de cobertura usando o BoustrophedonCore. A API pública é `BoustrophedonDotnetWrapper.Boustrophedon.GenerateWaypoints(Point start, Point end, Point[] area, Point[][] obstacles, double width, double overlapPercent)`.

`overlapPercent` deve estar no intervalo `[0, 100)`; 100 não é aceito porque elimina a distância entre faixas. `width` deve ser positivo e representa a largura total de atuação. A função devolve os pontos do percurso incluindo `start` e `end`.

`area` deve conter os vértices de um polígono simples com pelo menos três pontos. `obstacles` pode ser vazio; quando usado, cada obstáculo deve ser um polígono sem interseção com os demais e estritamente dentro da área. Os pontos devem usar as mesmas unidades cartesianas. Entradas inválidas, falhas nativas e erros de alocação são comunicados como exceções .NET.

## Uso em um projeto .NET

Suporte inicial: .NET 10 em Linux x64 com glibc, usando Debian 12 como base de compilação. Alpine/musl, Windows, macOS e ARM64 não estão incluídos. O consumidor precisa do runtime .NET 10 e das bibliotecas de sistema glibc/libm; não precisa de CMake nem Docker.

Instale o pacote local no projeto consumidor:

```sh
dotnet add package BoustrophedonDotnetWrapper --version 0.1.0 --source /caminho/absoluto/artifacts/packages
```

Chame a API com `Point`, um tipo imutável de coordenadas `double`:

```csharp
using BoustrophedonDotnetWrapper;

Point[] route = Boustrophedon.GenerateWaypoints(
    new Point(0, 0), new Point(10, 10),
    [new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10)],
    [], width: 2, overlapPercent: 10);
```

## Pré-requisitos

- .NET SDK 10.
- Git e CMake para compilar localmente.
- Docker para gerar a biblioteca distribuível baseada em Debian 12.
- Submódulo `native/BoustrophedonCore` no commit `2d3af904cd785b558b9e7bd5fc9f4a7103f2042e`.

Inicialize o submódulo após obter o repositório do wrapper:

```sh
git submodule update --init --recursive
```

O script valida o commit e exige o worktree do core limpo antes de compilar. Ele cria um arquivo temporário com `git archive` do commit fixado e compila apenas esse conteúdo. Alterações locais no core não são executadas.

## Compilar a biblioteca nativa

Para desenvolvimento, use o toolchain do host:

```sh
./scripts/build-native.sh --mode=host
```

Para distribuição, use o baseline Debian 12 x64:

```sh
./scripts/build-native.sh --mode=container
```

Ambos geram o arquivo real `artifacts/native/linux-x64/libboustrophedon.so` e `build-info.txt`, com SHA-256 da biblioteca. A metadata do modo container identifica o commit e a imagem `debian:12-slim`; `scripts/pack.sh` sempre refaz essa biblioteca no container antes do pacote Release. `scripts/check-native.sh` valida esse vínculo e rejeita builds de host na distribuição.

## Gerar pacote NuGet

```sh
./scripts/pack.sh
```

O comando compila o projeto Release e grava os pacotes em `artifacts/packages/`. A biblioteca é incluída em `runtimes/linux-x64/native/libboustrophedon.so`, e a origem e o hash ficam em `build-info/build-info.txt`. A resolução nativa funciona sem `LD_LIBRARY_PATH`. Mesmo `dotnet pack` direto verifica a origem do binário e rejeita artefatos de desenvolvimento gerados no host.

## Verificar um pacote NuGet

```sh
./scripts/verify-package.sh artifacts/packages/BoustrophedonDotnetWrapper.0.1.0.nupkg
```

O script cria um consumidor temporário que referencia apenas o pacote, restaura em cache NuGet isolado, executa uma chamada à API e também publica e executa para `linux-x64`. O wrapper é restaurado exclusivamente do feed local; nuget.org fornece somente os pacotes Microsoft de runtime/targeting, quando necessários pelo SDK do host. A execução remove `LD_LIBRARY_PATH`.

## Testes

```sh
./scripts/build-native.sh --mode=host
dotnet test
```

Os testes usam a API pública com o core real. Para executar a suíte C existente:

```sh
cmake -S native/BoustrophedonCore -B artifacts/core-tests -DCMAKE_BUILD_TYPE=Release
cmake --build artifacts/core-tests --parallel 2
ctest --test-dir artifacts/core-tests --output-on-failure
```

O diagnóstico de memória roda um consumidor da API sob Valgrind em um container separado:

```sh
./scripts/check-memory.sh 1000
```

O relatório fica em `artifacts/memory/valgrind.xml`. Consulte [os resultados e limites da validação](docs/memory-validation.md).

## Contrato e falhas

O método é síncrono. Os arrays de entrada são preservados; não os modifique durante a chamada. Cada retorno é um array gerenciado independente, e toda memória do resultado nativo é liberada pelo wrapper. Chamadas independentes podem ocorrer em paralelo.

| Condição | Exceção |
|---|---|
| Array principal nulo | `ArgumentNullException` |
| Largura ou sobreposição fora da faixa, ou não finita | `ArgumentOutOfRangeException` |
| Coordenadas, polígonos ou geometria inválidos | `ArgumentException` |
| Caminho não encontrado ou status nativo desconhecido | `InvalidOperationException` |
| Limite de representação, incluindo espaçamento que sofre underflow | `OverflowException` |
| Falha de alocação gerenciada ou nativa | `OutOfMemoryException` |
| Processo fora de Linux x64 | `PlatformNotSupportedException` |

Falhas de carregamento da biblioteca preservam as exceções de interop do .NET. O pacote é destinado a glibc; a checagem de sistema/arquitetura não detecta musl antecipadamente.

## Geometria e limites

Os anéis aceitam ambos os sentidos e um vértice de fechamento repetido. Auto-interseções, vértices consecutivos duplicados e obstáculos que se tocam, se sobrepõem ou se aninham são rejeitados. Start/end precisam estar no domínio livre; contornos são transitáveis.

A cobertura usa disco de diâmetro `width`, com espaçamento `width * (1 - overlapPercent / 100)`. A varredura é vertical, sem busca de ângulo. O algoritmo é determinístico e heurístico, sem garantia de mínimo global; conectores podem revisitar células. Não há conversão geográfica, raio físico do agente, raio mínimo de curva ou cancelamento.

O core usa geometria de ponto flutuante. Coordenadas muito grandes em relação aos detalhes podem exigir recentralização/reescala. Muitos vértices ou espaçamentos muito pequenos podem exigir CPU e memória consideráveis; não há garantia de tempo para entradas arbitrárias.

## Exemplo

O projeto [examples/ConsoleExample](examples/ConsoleExample) mostra uma chamada direta à API. Depois de gerar a biblioteca nativa, execute:

```sh
dotnet run --project examples/ConsoleExample/ConsoleExample.csproj
```
