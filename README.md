# AnythingLLM RAG - MAUI App

![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)

Una aplicación multiplataforma construida con **.NET MAUI** que se integra con **AnythingLLM** para proporcionar capacidades de chat inteligentes basadas en tus propios documentos gracias a la tecnología RAG (Retrieval-Augmented Generation).

## ✨ Características Principales

*   **Gestión de Workspaces:** Crea, edita y elimina espacios de trabajo directamente desde la aplicación para mantener tus contextos organizados.
*   **Modos de Chat Duales:**
    *   **💬 Modo Chat:** Mantén conversaciones prolongadas donde la IA recuerda el contexto del historial (respaldado por la API de AnythingLLM).
    *   **🔍 Modo Query:** Realiza consultas directas a tus documentos sin mantener memoria de interacciones previas.
*   **Gestión de Documentos:**
    *   Sube documentos locales al almacén global de AnythingLLM.
    *   Añade y quita documentos de tus *workspaces* dinámicamente.
*   **Personalización:** Configura el *System Prompt* (instrucciones base de la IA) de cada workspace según tus necesidades.
*   **Interfaz Moderna e Intuitiva:** UI reactiva con auto-scroll integrado y notificaciones visuales del estado de las peticiones.

## 🚀 Requisitos Previos

Para ejecutar y compilar este proyecto necesitarás:

1.  **[Visual Studio 2022](https://visualstudio.microsoft.com/)** (Windows) con la carga de trabajo de *Desarrollo de la interfaz de usuario de aplicaciones multiplataforma de .NET (.NET MAUI)* instalada.
2.  **Un servidor de [AnythingLLM](https://useanything.com/)** en funcionamiento (local o en la nube).
3.  Una **Clave de API** (API Key) generada en tu instancia de AnythingLLM.

## 🛠️ Configuración e Instalación

1.  **Clona este repositorio** en tu máquina local.
2.  Abre la solución (`Rag.sln`) en Visual Studio.
3.  **Configura las credenciales de la API:**
    *   Navega al archivo: `Services/AnythingLlmService.cs`.
    *   Asegúrate de que la URL base apunte a tu instancia de AnythingLLM (por defecto: `http://localhost:3001/api/v1/`).
    *   Actualiza el Bearer Token en los headers con tu clave de API si es necesario, o configúrala en el arranque de la app si está parametrizado.
4.  Selecciona el target deseado (por ejemplo, `Windows Machine` o un emulador de Android/iOS) en la barra de ejecución.
5.  Pulsa **F5** para compilar y ejecutar la aplicación.

## 📂 Estructura del Proyecto (MVVM)

El proyecto sigue el patrón de arquitectura **Model-View-ViewModel (MVVM)**:

*   **`Models/`**: Clases de datos como `Workspace`, `ChatMessage` y `Document`.
*   **`ViewModels/`**: Lógica de presentación, gestión de estado y comandos (ej. `MainViewModel.cs`), apoyados por *CommunityToolkit.Mvvm*.
*   **`Views/`**: Interfaces de usuario escritas en XAML y su Code-Behind (`MainPage.xaml`).
*   **`Services/`**: Servicios de infraestructura para comunicarse con el backend, como `AnythingLlmService.cs`.

## 🤝 Contribuciones

Este es un proyecto educativo / de portafolio, pero las sugerencias, correcciones de errores o mejoras ("Pull Requests") son bienvenidas.

---
