# Visão Geral

Este projeto é um wrapper da implementação do algoritmo Boustrophedon desenvolvido em C. A implementação desenvolvida é o BoustrophedonCore. O objeto desse wrapper é apresentar uma API para uma linguagem de alto nível, nesse caso, o ambiente .NET. Esse wrapper deverá ser um pacote do .NET.

# Assinatura da função

A implementação deverá ter uma função de ponto de entrada, ou seja, uma única função que outros código irão chamar. Isso não significa que todo o código deve ficar em uma única função. Abaixo o pseudo código da assinatura da função.

```
generateWaypoints(Ponto Inicial, Ponto Final, Vertices da Area, Vertices dos Bloqueios, Largura de atuação, Sobreposição): Caminho;
```
**Ponto Inicial**: Representado por dois valores, {x: 10, y: 10}. Será de onde o agente autonomo irá começar o percurso.
**Ponto Final**: Representado por dois valores, {x: 10, y: 10}. Será de onde o agente autonomo irá terminar o percurso.
**Vertices da Area**: Representado por vários valores, [{x: 10, y:10}, {x:10, y: 9}]. Representa os vertices do poligono a ser percorrido.
**Vertices dos Bloqueios**: Representado por vários valores, [[{x: 10, y:10}, {x:10, y: 9}], [{x: 10, y:10}, {x:10, y: 9}]]. Representa os vertices do obstáculos.
**Largura de atuação**: Representado por um único valor, 10. Representa a largura total da visão do agente autonomo, valor da direita + valor da esquerda.
**Sobreposição**: Representado por um único valor, 0 a 100. Representa o quanto as larguras de visão devem se sobrepor.
**Caminho**: Representado por vários valores, [{x: 10, y:10}, {x:10, y: 9}]. Representa o caminho gerado pelo algoritmo.

Está deverá ser a única função pública do Wrapper. Ela estará contida na classe Boustrophedon.

# Práticas de codificação

- Utilizar convetional commits e sempre commitar quando tiver terminado a menor unidade de uma atividade funcional.
- Utilizar nomes de variáveis, funções etc de forma que fiquem legíveis.
- Utilizar TDD para codificar primeiro um teste funcional e depois criar a funcionalidade.