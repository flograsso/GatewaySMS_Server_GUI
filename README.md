# UI_Server_GatewaySMS
Servidor HTTP en C# el cual escucha constantemente un puerto dado y al recibir un POST, crea un nuevo Thread que lo maneja. 
Este Thread recibe los parametros de un mensaje y lo encola. 
Otro Thread se encarga de desencolar los mensajes y enviarlos por un modulo GSM mediante el puerto serial

Los parametros a configurar para su funcionamiento son:

En el archivo TCPServer, hay que setear la IP local de la PC en la variable "DEFAULT_SERVER" y el puerto en "DEFAULT_PORT"
El BaudRate para comunicarse con el GSM esta configurado en 115200, se puede editar en la clase GSM_Module, en la linea "this.serialPort.BaudRate=115200;"

-Setear una IP fija en la PC que se instale. El software detecta automáticamente la IP de dicha PC y levanta el servicio en esta IP.
-Abrir el puerto 31001 en el router redirigiendolo a dicha IP
-El software utiliza una herramienta de windows para gestionar los dispositivos del sistema desde la consola, la misma se llama "devcon"
En la carpeta raiz se puede encontrar un rar con la herramienta. La misma debe ser extraida en la misma carpeta que el .exe del Gateway
Se debe corroborar que esta herramienta funcione en el SO en que se esta ejecutando. 
Para ello desde la consola me muevo a la carpeta donde se encuentra el programa y ejecuto "devcon.exe restart *PID_XXXX*" 
Donde XXXX es el numero de instacia del dispositivo que lo puedo sacar del administrador de dispositivos de windows
