# Практика 4: запуск сервера на VPS

## Сборка сервера

Открой `Assets/Scenes/MainScene.unity` и запусти в Unity:

`Practice4 -> Build Linux Dedicated Server`

Сервер будет собран сюда:

`Builds/LinuxServer/practica4_server.x86_64`

Можно собрать и из командной строки:

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Users\antya\Desktop\Multiplayer-main\practica4_multipleer" `
  -executeMethod DedicatedServerBuilder.BuildLinuxServer
```

## Запуск на VPS

Скопируй папку `Builds/LinuxServer` на VPS и запусти:

```bash
chmod +x practica4_server.x86_64
./practica4_server.x86_64 -batchmode -nographics -port 7770
```

На VPS нужно открыть UDP-порт `7770`.

## Подключение клиента

В клиенте введи публичный IP или DNS VPS в поле `IP или DNS сервера` и нажми `Client`.

Также можно передать адрес аргументами:

```powershell
GameClient.exe -address YOUR_VPS_IP -port 7770
```

## Логика

- Headless-сервер стартует сам в `batchmode`.
- Лобби ждет `2` игроков.
- Матч идет `60` секунд.
- За убийства начисляется счет.
- После таймера показываются результаты.
- Затем сервер сбрасывает здоровье/счет и возвращает всех в лобби.

