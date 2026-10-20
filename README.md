# Mais plataformas — Mad Games Tycoon 2

Mod BepInEx 5, versão **1.2.0**, para escolher **4 a 16 plataformas no mesmo projeto**, com padrão 8.
Feito contra a instalação Steam build `20855306`, Unity 2022.3.2, BepInEx 5.4.23.3.

## Como usar

1. Ao criar uma campanha, avance até a tela de configurações/mapa.
2. Ajuste **Máximo de plataformas por jogo** nos botões `−` e `+`, abaixo do título,
   e confirme a criação da campanha. Não é necessário apertar F8 para essa configuração.
3. Ao desenvolver um jogo, escolha as plataformas nos cartões da interface normal.
4. **Role a área de plataformas com a roda do mouse ou com a barra lateral** para ver
   as plataformas 5 em diante. Os cartões continuam em duas colunas.
5. Clique no cartão para abrir o seletor original e no botão de remover para limpar a escolha.

O limite fica gravado **por campanha**, no próprio save, e é restaurado ao carregar.
O arquivo `BepInEx/config/br.sapo.mgt2.moreplatforms.cfg`, opção
`MaximoDePlataformas`, define o padrão para novas campanhas e saves antigos sem essa informação.
Cancelar a criação da campanha não muda o limite da campanha atual.

O painel F8 continua disponível apenas como atalho opcional para alterar o limite durante
uma campanha. A seleção das plataformas extras está integrada à tela normal.
Alterações na campanha são persistidas quando você salva o jogo.

Diminuir o limite não altera jogos já criados. Se o projeto ainda aberto tiver plataformas
selecionadas acima do novo limite, o painel pede que elas sejam removidas primeiro.

## Instalação e remoção

Com o jogo fechado, copie `build/MorePlatforms.dll` para `BepInEx/plugins/`, ou execute:

```powershell
.\install.ps1
```

O mod `UnlimitedPorts.dll` existente faz outra coisa (ports posteriores) e não é substituído.
O executável e as DLLs originais do jogo não são alterados.

Para desativar, mova `MorePlatforms.dll` para fora de `BepInEx/plugins` com o jogo fechado.
**Não salve uma campanha com plataformas extras após remover o mod:** o jogo original
grava apenas quatro plataformas e descartará os dados extras ao salvar novamente.
Mantenha uma cópia do save anterior ao primeiro uso e faça o primeiro teste em outro slot.

## Escopo e limites

- Desenvolvimento em single-player, com os filtros, devkits e requisitos originais.
- Contratos, jogos exclusivos e arcades não recebem plataformas extras.
- Os custos e cálculos de vendas que percorrem as plataformas passam a receber a lista ampliada.
- Jogos de NPCs não ganham slots extras automaticamente.
- As plataformas adicionais são gravadas em uma chave própria dentro do mesmo arquivo de save.
- Algumas telas antigas continuam mostrando apenas os quatro ícones originais; listas textuais
  que percorrem todas as plataformas podem mostrar as extras.
- A tela original de trocar plataformas durante o desenvolvimento preserva as extras,
  mas só permite editar os quatro primeiros slots.
- Bundles e alguns bônus/estatísticas especializados continuam usando as regras originais
  de quatro plataformas. Não é uma revisão completa de todos os sistemas do jogo.
- Multiplayer não é suportado. Não use uma campanha com plataformas extras em multiplayer.
- O mod não acessa a internet nem baixa outros componentes.

## Compilar e verificar

Usa o compilador C# do Windows e as referências da instalação local, sem baixar pacotes:

```powershell
.\build.ps1
.\tests\verify.ps1
```

É possível passar `-GamePath 'caminho do jogo'` aos scripts.
O código do mod está em `src/`. Os arquivos privados de análise em `.work/` e as
ferramentas em `.tools/` não fazem parte da distribuição.

## Teste dentro do jogo

1. Em uma cópia da campanha, configure 8 e selecione pelo menos cinco plataformas distintas.
2. Confira que o custo muda ao adicionar e remover a quinta.
3. Inicie o desenvolvimento e abra o relatório para verificar que não há erro de interface.
4. Salve em outro slot, volte ao menu e carregue esse slot.
5. Confira as plataformas na descrição/tooltip do jogo e prossiga até o lançamento.
6. Experimente reduzir para 4: os jogos existentes devem manter as plataformas extras.

Compilação e verificações estáticas não substituem esse teste de funcionamento.

## Distribuição e GitHub Actions

Execute `./package.ps1` para gerar os ZIPs de instalação e fontes em `dist/`.
O download de jogadores já contém `BepInEx/plugins/MorePlatforms.dll`.
Consulte [o guia de publicação](docs/PUBLISHING.md) para preparar a página na Nexus,
configurar as referências privadas do build e gerar um rascunho de release por tag.
