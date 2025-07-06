# 🧟 2025ZombisEM

**Juego multijugador desarrollado en Unity** para la asignatura de *Entornos Multijugador* (3º Diseño y Desarrollo de Videojuegos), usando **Netcode for GameObjects (NGO)** y **Unity Relay**. Se trata de una experiencia entre humanos y zombis, donde los jugadores deben colaborar, competir y sobrevivir.


## Tecnologías utilizadas

- **Unity**
- **Netcode for GameObjects (NGO)** – Networking con autoridad del servidor
- **Unity Relay** – Comunicación sin necesidad de abrir puertos
- **C#**


## Mecánicas del juego

- Dos roles: **Humanos** y **Zombis**
- Modo de juego seleccionable por el host:
  - **Victoria por tiempo**: sobrevive hasta que se agote el tiempo.
  - **Victoria por monedas**: recoge una cantidad determinada de monedas.
- Los zombis deben atrapar a los humanos para convertirlos.
- Si todos los humanos son zombificados → victoria zombi.
- Si se cumple una condición de victoria → victoria humana.


## Flujo general

1. **Lobby** con interfaz para introducir nombre, elegir rol (host/cliente), y configurar partida.
2. El host configura:
   - Modo de juego
   - Tiempo límite
   - Densidad de monedas
3. Cuando todos los jugadores están listos:
   - Se cambia de escena automáticamente.
   - El host instancia jugadores y escenario con una semilla compartida.
4. ¡Comienza el Juego!


## Arquitectura de red

- Basado en un **modelo cliente-servidor** con host autoritario.
- Sincronización de estado mediante:
  - `NetworkVariable` para datos persistentes.
  - `ServerRpc` y `ClientRpc` para eventos.
- Uso de `Relay` para facilitar conexiones NAT-traversal sin puertos abiertos.


## Principales funcionalidades técnicas

- **Sincronización de posición y rotación**: envío manual vía `ClientRpc` (en vez de `NetworkTransform`, para mayor control).
- **Spawn y despawn de jugadores y objetos** mediante `NetworkObject`.
- **Transformación dinámica de humano a zombi**: despawn del jugador original y spawn del prefab zombi conservando información de estado.
- **Recogida de monedas sincronizada** entre todos los clientes.
- **Cambio de escena sincronizado** cuando todos están listos.
- **Persistencia entre escenas** usando el patrón `Singleton` para el `GameManager`.


## Scripts destacados

- `NetworkEvents.cs`: conexión y desconexión de jugadores.
- `UIManager.cs`: manejo de la interfaz en el lobby y juego.
- `GameManager.cs`: almacenamiento de configuración y estado de partida.
- `LevelManager.cs`: control de spawn, eventos de victoria, etc.
- `ZombieCollisionHandler.cs`: detección de colisión y conversión.
- `NamePlayers.cs`: orientación dinámica de nombres hacia la cámara.


## Detalles adicionales

- **HUD funcional** y dinámico según rol.
- **Diccionario de nombres** de jugadores sincronizado mediante RPC.
- **Reinicio de partida** sin necesidad de cerrar el servidor.
- **Mapas aleatorios** sincronizados con una semilla compartida.


## Conclusiones

Este proyecto sirvió como introducción práctica a la programación multijugador en Unity usando Netcode for GameObjects. Abordamos los aspectos técnicos de sincronización y arquitectura cliente-servidor, logrando una experiencia cooperativa, competitiva y divertida.
