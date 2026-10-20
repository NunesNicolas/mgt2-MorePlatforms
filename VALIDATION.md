# Validação — 20/10/2026

## Versão 1.2.0 — plataformas com rolagem

- Compilação, 18 alvos declarativos Harmony e 12 testes de persistência aprovados.
- Teste dentro do Unity com os elementos originais: 16 cartões criados; callbacks dos
  slots 5, 8 e 16 encaminham seleção e remoção ao índice correto.
- Contagens 4, 7 e 16 verificadas; barra oculta com quatro; cartões excedentes
  ocultos ao reduzir; reconstrução não duplica cartões; callbacks originais preservados.
- Prévias renderizadas da página no início e no fim da rolagem, inspecionadas localmente.
  O diagnóstico usa uma cópia da interface, sem carregar campanha.
- Seleção por F8 substituída pelos cartões nativos; F8 permanece um atalho opcional
  para editar o limite.
- O diagnóstico intercepta os botões para verificar seus índices. Ainda é necessário
  testar o fluxo completo de seleção, desenvolvimento e lançamento numa campanha.

## Versão 1.1.0 — configuração na criação da campanha

- Compilação e 12 testes de persistência passaram novamente.
- 17 alvos declarativos Harmony encontrados.
- Teste dentro do processo Unity, sem renderização: criação da linha nativa no menu
  `Menu_NewGameSettings`, altura adicional de 44 unidades, reabertura sem duplicação.
- Acionamento dos callbacks dos botões: limites 4/16 respeitados; callbacks originais
  não executados; valor pendente não altera a campanha ativa.
- Escrita e leitura reais pelo ES3 em arquivo isolado: limite 11 restaurado depois
  de mudar o estado ativo para 5.
- Log registrou `CAMPAIGN_UI_TEST_PASS` e carregamento da versão 1.1.0.
- Diagnóstico temporário removido após o teste. Nenhum save de campanha foi carregado.
- Ainda não houve inspeção visual renderizada da nova linha nem teste completo de
  criação de campanha por cliques do usuário.

## Versão 1.0.0

- Compilação concluída contra as DLLs locais do jogo e BepInEx.
- 12 testes do formato de persistência aprovados: dados vazios, vários jogos,
  espaços não preenchidos, limite de 16 e rejeição de registros truncados/inválidos.
- 13 alvos declarativos Harmony encontrados na instalação.
- Sete métodos com quatro ícones verificados contra os padrões de IL esperados.
- Inicialização real do jogo em modo `-batchmode -nographics`:
  BepInEx carregou `Mais plataformas 1.0.0` e `Unlimited Ports Mod 1.0.0`;
  o novo mod confirmou a aplicação dos patches e limite 8.
- O processo de teste foi encerrado após 20 segundos, sem carregar campanha.
- Defender não detectou ameaças na DLL compilada.
- Backup local da pasta de dados do jogo: `backups/saves-before-moreplatforms`,
  incluindo 41 arquivos `.sav`.

**Ainda não validado interativamente:** desenho e interação do painel F8,
desenvolvimento e lançamento com mais de quatro plataformas, custos observados,
salvar/carregar uma campanha e interações completas com o mod UnlimitedPorts.
O carregamento conjunto não prova compatibilidade de todas as funcionalidades.

O teste sem renderização gera avisos de shaders sem GPU; isso não equivale a
um teste gráfico. A varredura do Defender também não é garantia de risco zero.
