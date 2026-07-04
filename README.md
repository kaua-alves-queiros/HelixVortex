# 🌀 Helix Vortex

Um jogo de arcade 3D eletrizante e dinâmico inspirado no clássico *Helix Jump*, desenvolvido utilizando **MonoGame** e **.NET 9**.

![Gameplay Screenshot](Icon.bmp) *(Ícone ilustrativo do jogo)*

---

## 🎮 Como Jogar

O objetivo é guiar a bola saltitante até a base da torre cilíndrica, passando pelas aberturas dos anéis e desviando dos obstáculos mortais.

* **Rotacionar a Torre:**
  * Teclado: Pressione **Seta Esquerda** / **A** para girar a torre para a direita e **Seta Direita** / **D** para girar para a esquerda.
  * Mouse: Clique com o **Botão Esquerdo** e arraste para os lados para rotacionar livremente.
* **Saltar e Cair:** A bola pula automaticamente ao atingir as fatias seguras.
* **Obstáculos:** Evite fatias **vermelhas** (obstáculos fatais) a todo custo! Atingi-las causará Game Over.
* **Vortex Mode:** Ao atravessar **2 ou mais anéis consecutivamente** sem tocar em nada, a bola ganha velocidade extrema e entra no **Modo Vortex** (rastro de fogo). Nesse estado, ela destrói o próximo anel que tocar (inclusive fatias de obstáculo vermelhas) e concede pontuação bônus massiva!

---

## ✨ Características Técnicas & Visuais

* **Motor Gráfico:** Renderização 3D de alta performance construída diretamente sobre o **MonoGame**.
* **Geometria Procedural:** Fatias de anéis e cilindro central gerados programaticamente via código (`MeshBuilder.cs`), com faces internas e laterais completas para sensação de solidez (`CullNone`).
* **Efeitos de Partículas:**
  * Rastro dinâmico da bola (faíscas neon na queda normal, fogo intenso no modo Vortex).
  * Impactos de colisão realistas com splash de cor.
  * Estilhaçamento dinâmico de anéis com física procedural para cada pedaço.
* **Câmera Dinâmica:** Movimento suave com interpolação linear (`Lerp`) e efeito de tremor de câmera (`Camera Shake`) baseado na intensidade do impacto ou destruição.
* **Design Visual Premium:** Fundo em gradiente suave com cores Tailored (HSL) dinâmicas por fase e fontes em estilo retrô pixelizado feitas sob medida.
* **Persistência de Recorde:** O recorde pessoal (`High Score`) é salvo e lido automaticamente a partir de um arquivo local (`highscore.txt`).

---

## 🚀 Como Executar

### Pré-requisitos
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instalado em sua máquina.

### Passos
1. Abra o terminal na pasta raiz do projeto:
   ```bash
   cd /Users/kaua/Source/kaua-alves-queiros/Games/HelixVortex
   ```
2. Restaure as dependências e execute o jogo:
   ```bash
   dotnet run
   ```

---

## 🛠️ Arquitetura do Projeto

* `Game1.cs`: Loop principal do jogo (Inicialização, Update, Draw e gerenciamento de estados).
* `Entities/`
  * [Ball.cs](file:///Users/kaua/Source/kaua-alves-queiros/Games/HelixVortex/Entities/Ball.cs): Física da bola, estados de pulo/vortex e efeitos visuais da bola.
  * [Tower.cs](file:///Users/kaua/Source/kaua-alves-queiros/Games/HelixVortex/Entities/Tower.cs): Estrutura da torre, geração de níveis dinâmica, rotação e detecção fina de colisão de fatias.
* `Rendering/`
  * [MeshBuilder.cs](file:///Users/kaua/Source/kaua-alves-queiros/Games/HelixVortex/Rendering/MeshBuilder.cs): Geração matemática de cilindros e prismas procedurais.
  * [ParticleSystem.cs](file:///Users/kaua/Source/kaua-alves-queiros/Games/HelixVortex/Rendering/ParticleSystem.cs): Sistema de partículas CPU eficiente para rastros e explosões.
  * [PixelFontRenderer.cs](file:///Users/kaua/Source/kaua-alves-queiros/Games/HelixVortex/Rendering/PixelFontRenderer.cs): Desenhador de fonte bitmap sem dependência de SpriteFont para visual retrô pixel-perfect.
