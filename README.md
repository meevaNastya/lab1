# Отчёт по лабораторной работе: развёртывание Car Rental в Kubernetes
## Демонстрация: https://drive.google.com/drive/folders/1-72ke8lE2BFdXNjwXfGMLkEpNVi57Vhn

В видео последовательно показаны:
1. Состояние Minikube и наличие Docker-образа
2. Deployment и 3 работающие реплики приложения
3. Работа Metrics Server (`kubectl top pods`)
4. Реакция HPA на нагрузку рост реплик 
5. Дашборд Grafana с графиком роста CPU в реальном времени
   
## Цель

Развернуть готовое приложение Car Rental в Kubernetes с использованием Minikube: ознакомиться с базовыми
концепциями Kubernetes, настроить автоматическое масштабирование и
мониторинг нагрузки.(В качестве основы для лабораторной работы используется уже реализованный проект Car Rental  backend-система аренды автомобилей на ASP.NET Core с PostgreSQL.) 

Бизнес-логика приложения в рамках этой работы не менялась  выполнялся
только деплой уже существующего кода.


### 1. Развёрнут Minikube

Локальный однонодовый кластер поднят командой `minikube start`
(драйвер Docker, container runtime — containerd).

Проверка:
```bash
minikube status
```

### 2. Создан Docker-образ приложения

Написан multi-stage `Dockerfile`:
- стадия сборки — `mcr.microsoft.com/dotnet/sdk:8.0` (`dotnet publish`);
- стадия запуска — `mcr.microsoft.com/dotnet/aspnet:8.0`.

Образ собирается напрямую внутри кластера (без внешнего registry):
```bash
minikube image build -t car-rental-api:local .
```

Для корректной работы в контейнере в `Program.cs` внесены два изменения:
- добавлено автоматическое применение EF Core миграций при старте
  (`Database.MigrateAsync()`) исходно миграции применялись только вручную
  командой `dotnet ef database update`, что не подходит для пода без
  установленного `dotnet-ef`;
- добавлен эндпоинт `/health`, используемый readiness/liveness пробами -
  Swagger, который использовался локально для проверки работоспособности,
  недоступен вне Development-окружения.

### 3-4. Создан Deployment, установлено 3 реплики

`k8s/api-deployment.yaml`:
- `replicas: 3`;
- конфигурация и секреты подключены через `ConfigMap` (`k8s/configmap.yaml`)
  и `Secret` (`k8s/secret.yaml`, создаётся из `secret.example.yaml`);
- заданы `resources.requests`/`resources.limits` по CPU и памяти 
  обязательное условие для работы HPA;
- readiness/liveness пробы на `/health`.

PostgreSQL развёрнут отдельным Deployment с `PersistentVolumeClaim`
(`k8s/postgres.yaml`), доступен внутри кластера по DNS-имени `postgres`.

Проверка:
```bash
kubectl get deployment car-rental-api
kubectl get pods
```

### 5. Установлен Metrics Server

```bash
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

Для работы в Minikube потребовался патч:
```bash
kubectl patch deployment metrics-server -n kube-system --type='json' \
  -p='[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
```

Проверка:
```bash
kubectl get pods -n kube-system | grep metrics-server
kubectl top pods
```

### 6. Настроен Horizontal Pod Autoscaler

`k8s/hpa.yaml`: `minReplicas: 2`, `maxReplicas: 5`, целевая загрузка CPU — 50%.

Под искусственной нагрузкой (параллельные запросы к `/api/cars` через
`kubectl port-forward`) загрузка CPU превышала порог (зафиксировано до
230% от целевого значения), и HPA автоматически увеличивал число реплик
с 2 до 5. После остановки нагрузки количество реплик возвращалось к
минимальному значению.

при первом тесте под нагрузкой новые
поды падали с ошибкой PostgreSQL `sorry, too many clients already` — все
реплики одновременно открывали избыточное число соединений к БД.
Исправлено:
- `Maximum Pool Size=20` в строке подключения добавлено ограничение пула на под;
- `max_connections=200` у PostgreSQL (параметр `args` в `postgres.yaml`).

### 7. Настроен мониторинг Prometheus + Grafana

Установлены через Helm:
```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install prometheus prometheus-community/kube-prometheus-stack
```

Для визуализации нагрузки использован встроенный дашборд
**Kubernetes / Compute Resources / Pod**: он отображает CPU и память
конкретного пода `car-rental-api` в реальном времени в сравнении с
заданными `requests`/`limits`. Во время нагрузочного теста на графике
виден рост потребления CPU, синхронный с масштабированием HPA.



## Структура репозитория

```
CarRental.Api/       — код приложения
CarRental.Tests/     — тесты
Dockerfile           — сборка образа приложения
.dockerignore
k8s/
  configmap.yaml         — несекретные настройки
  secret.example.yaml    — шаблон секретов (реал secret.yaml в .gitignore)
  postgres.yaml          — PostgreSQL: Deployment + PVC + Service
  api-deployment.yaml    — Deployment приложения (3 реплики)
  api-service.yaml       — Service для доступа к API
  hpa.yaml               — HorizontalPodAutoscaler
  README.md              — инструкция по деплою
```
