# Publicar na Nexus e gerar releases no GitHub

## Para quem instala

O download principal e `MorePlatforms-X.Y.Z.zip`. O jogador instala BepInEx 5
Windows x64 uma vez e extrai o ZIP do mod na pasta do jogo. A estrutura pronta e:

```text
Mad Games Tycoon 2/
  Mad Games Tycoon 2.exe
  BepInEx/
    plugins/
      MorePlatforms.dll
  MorePlatforms-README.txt
  MorePlatforms-manifest.json
```

Atualizar exige apenas substituir a DLL com o jogo fechado. O ZIP nao inclui
configuracoes pessoais, saves, UnlimitedPorts ou DLLs originais do jogo.
Nao precisa de instalador EXE, scripts, compilador ou Git para instalar.
Nao anuncie suporte a Vortex antes de testar a instalacao por ele.

## Preparar o primeiro download

```powershell
.\package.ps1
```

O script compila, verifica os patches, roda os testes de persistencia e gera em
`dist/` o ZIP para jogadores, outro de fontes e `SHA256SUMS.txt`.
O arquivo para a Nexus e **MorePlatforms-1.2.0.zip**, nao o arquivo `source`.

Antes da primeira publicacao, conclua o teste manual de criar um jogo com mais
de quatro plataformas, salvar, carregar e lancar. Os testes automatizados nao
substituem esse fluxo. A descricao preparada trata a versao inicial como teste.

## Criar a pagina na Nexus

1. Entre na sua conta, escolha Mad Games Tycoon 2 e abra o envio de um novo mod.
2. Use o nome **More Platforms - Configurable Platform Limit**.
3. Copie e adapte `docs/NEXUS-DESCRIPTION.txt`; informe que os textos do mod
   estao em portugues, que e single-player e quais versoes foram testadas.
4. Adicione BepInEx 5 Windows x64 como requisito externo, com o link oficial.
5. Envie `MorePlatforms-1.2.0.zip` como arquivo principal, versao `1.2.0`.
6. Adicione capturas reais da campanha mostrando configuracao e rolagem.
7. Escolha as permissoes/licenca, revise a pagina e publique.

Para atualizar a Nexus, adicione o novo ZIP na pagina existente e preencha o
changelog. A Action abaixo gera o arquivo; o envio a Nexus continua manual.
Nao ha automacao de login/upload por navegador neste projeto.

## Publicar os fontes no GitHub

Crie um repositorio para o mod e use o conteudo de
`dist/MorePlatforms-source-1.2.0.zip`. Esse ZIP inclui `.github/workflows/release.yml`.
O pacote de fontes usa uma lista explicita de arquivos permitidos; ele exclui
`.work`, `.tools`, `backups`, DLLs do jogo e saves.

Alternativamente, inicialize Git nesta pasta e revise os arquivos antes do push:

```powershell
git init
git add .gitignore .github src tests scripts docs README.md VALIDATION.md build-references.json build.ps1 install.ps1 package.ps1
git status
git commit -m "Prepare More Platforms 1.2.0"
git branch -M main
git remote add origin https://github.com/SEU-USUARIO/SEU-REPOSITORIO.git
git push -u origin main
```

Os comandos sao exemplos para voce executar depois de criar o repositorio.
Nada foi publicado ou enviado para uma conta durante a preparacao local.

## Configurar o build hospedado no GitHub

O plugin usa tipos de `Assembly-CSharp.dll`. Por isso, um runner limpo nao compila
somente com o codigo do mod. O workflow usa referencias privadas em um commit fixo.
Esses arquivos nao entram no repositorio publico nem nos artefatos publicados.

1. Execute localmente:

   ```powershell
   .\scripts\export-build-references.ps1
   ```

2. Crie um segundo repositorio **privado**, por exemplo `mgt2-build-references`.
   Se optar por armazenar as referencias no GitHub, coloque nele somente o conteudo
   de `private-build-references/`, mantendo a estrutura de pastas. Nao o torne publico.
   O script exporta apenas entradas de compilacao/verificacao, sem configs ou saves.
3. Anote o SHA completo (40 caracteres) desse commit.
4. Crie um token fine-grained limitado a esse repositorio privado, com **Contents:
   read-only**. Nao coloque o token em nenhum arquivo do projeto.
5. No repositorio do mod, em Settings > Secrets and variables > Actions, configure:

   | Tipo | Nome | Valor |
   | --- | --- | --- |
   | Variable | `BUILD_REFERENCES_REPO` | `SEU-USUARIO/mgt2-build-references` |
   | Variable | `BUILD_REFERENCES_SHA` | SHA completo do commit privado |
   | Secret | `BUILD_REFERENCES_TOKEN` | Token com acesso de leitura ao repositorio privado |

6. Em Actions, execute **Build release packages > Run workflow** para validar a configuracao.
   O ZIP aparecera em Artifacts. Essa execucao manual em uma branch nao cria release.

O workflow nao executa em pull requests. O build tem acesso de leitura; somente o
job de criar o rascunho recebe permissao de escrita nas releases. As actions sao
fixadas por SHA. As DLLs privadas sao usadas no runner temporario e nao sao anexadas
aos downloads. Um token expirado ou um SHA incorreto faz o build falhar explicitamente.

Se preferir manter as referencias apenas no seu PC, use `package.ps1` localmente e
anexe os ZIPs a uma release. Um runner Windows proprio tambem pode usar a instalacao
local, mas exige configurar e manter essa maquina; nao e necessario para a opcao acima.

## Publicar uma atualizacao

1. Atualize a versao do atributo `BepInPlugin` em `src/MorePlatforms.cs`, por exemplo `1.2.1`.
2. Atualize `README.md` e `docs/RELEASE-NOTES.md` e teste a mudanca no jogo.
3. Envie o commit e uma tag com a mesma versao:

   ```powershell
   git add src README.md docs tests scripts build.ps1 package.ps1
   git commit -m "Release 1.2.1"
   git push origin main
   git tag v1.2.1
   git push origin v1.2.1
   ```

4. A Action compila, verifica, empacota e cria um **rascunho de release** com ZIPs e hashes.
   Uma tag diferente da versao do codigo e rejeitada. O rascunho permite revisar o
   download antes de publica-lo. Uma release ja existente nao e sobrescrita.
5. Publique o rascunho e envie o ZIP de jogadores para a pagina existente na Nexus.

Quando o jogo ou BepInEx mudar, exporte novamente as referencias, crie outro commit
no repositorio privado e atualize `BUILD_REFERENCES_SHA` depois de testar a compatibilidade.

## Fontes oficiais

- Nexus: https://help.nexusmods.com/article/136-best-practices-for-mod-authors
- BepInEx testado: https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.3
- Checkout de repositorios privados: https://github.com/actions/checkout
- Eventos de Actions: https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows
- Criacao de rascunhos de release: https://cli.github.com/manual/gh_release_create
