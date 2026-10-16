# Validação de memória do wrapper .NET

`scripts/check-memory.sh [iterações]` constrói um consumidor .NET 10 e executa chamadas repetidas à API pública sob Valgrind Memcheck dentro de um container limitado a 1 GiB. O padrão são 1.000 iterações; o script aceita de 1 a 10.000.

O consumidor usa uma área quadrada de 100 × 100, sem obstáculos, largura 10, sobreposição 0% e início/fim nos cantos. Cada iteração chama `Boustrophedon.GenerateWaypoints`; o wrapper copia o caminho para managed memory e executa `freeWaypoints` no bloco `finally`. O core do submódulo fixado em `2d3af904cd785b558b9e7bd5fc9f4a7103f2042e` é reconstruído com símbolos de depuração no container para que os stacks de alocação identifiquem `libboustrophedon.so`.

O script exige que o submódulo esteja limpo e no commit fixado antes de iniciar o Docker. O Memcheck grava XML completo em `artifacts/memory/valgrind.xml`. O verificador falha para blocos definitivamente, indiretamente ou possivelmente perdidos, ou para `InvalidRead`, `InvalidWrite`, `InvalidFree` e `MismatchedFree`, quando o stack do erro contém `libboustrophedon.so`. Eventos sem esse frame ficam fora do critério do core; assim, a memória interna do runtime .NET não gera falhas por si só. Não há assertion sobre RSS ou uso máximo de memória.

## Resultado executado

Em 2026-10-16, `scripts/check-memory.sh 1000` terminou com sucesso usando `mcr.microsoft.com/dotnet/sdk:10.0-noble` e Valgrind 3.22.0. As 1.000 chamadas retornaram 26.000 waypoints no total (26 por chamada); o relatório não continha perdas nem erros de leitura/escrita/liberação atribuídos ao core. O XML completo fica em `artifacts/memory/valgrind.xml` após a execução.

Esse resultado cobre o cenário retangular descrito acima e o caminho normal de sucesso da API. Não demonstra a ausência de perdas em entradas com obstáculos, áreas inválidas ou falhas de alocação, nem caracteriza memória retida pelo runtime .NET. O filtro por stack significa que erros do runtime e alocações que o Valgrind não consiga atribuir ao objeto do core não são avaliados como erros do core.
