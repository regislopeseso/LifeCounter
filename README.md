# Exercício Api - Life Counter

Api para um site especializado em mostrar marcadores para contagem de vida

# Requisitos da Aplicação

### ETAPA 1 - Funções do Administrador

- Cadastro de jogo (nome e quantidade de vida padrao por jogador)
- Editar jogo
- Remover jogo

### ETAPA 2 - Funções do Jogador

- Iniciar jogo informando: jogo, nr de jogadores, informar se cada jogador vai utilizar o numero de vida padrao ou se vai ser um valor personalizado
- Adicionar 1 de vida a um jogador especifico
- Remover 1 de vida de um jogador em especifico
- Setar a vida de um jogador em especifico para o valor informado
- Resetar todos os jogadres para sua vida inicial
- Status do jogo: informar quanto de vida cada jogador tem atualmente, informar tempo da partida, informar se o jogo (ativo/finalizado) (se no momento dessa checagem apenas um jogador estiver vivo, ou todos estiverem mortos, a partida deve ser finalizada automaticamente)
- Finalizar partida: marcar a partida como finalizada, independente da vida dos jogadores

### ETAPA 3 - Função de Estatística

- Estatísticas gerais:
    - Total de partidas jogadas
    - Média de jogadores por partida
    - Duração média das partidas
    - Jogo mais jogado
    - Jogo mais demorado (maior duração média)
