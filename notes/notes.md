# Notas para consulta

## Docker Compose
```sh
# Subir todos os container sem prender o terminal
$ docker-compose up -d

# Para todos os containers do docker compose
$ docker-compose down

# Para os containers e deletar as imagens e containers
$ docker-compose down --rmi all
```

## Acessar banco
```sh
# Depois de rodar o compose
$ docker exec -it rinhabackend-db-1 psql -U admin -d rinha

$ explain analyze SELECT SEL_PESSOA_TERMO('pyt');
```

## Acessar redis
```sh
$ redis-cli

# Retorna todas as keys
$ keys *

# Retorna os valores da key
$ get uma-chave-aqui

# Deleta tudo em tudo
$ FLUSHALL
```