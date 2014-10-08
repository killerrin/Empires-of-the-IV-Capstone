#pragma once
// Define Anarian_DirectX_Mode for DirectX
// Define Anarian_OpenGL_Mode for OpenGL


namespace Anarian
{
	class AnarianGameEngine
	{
	private:
	protected:
		GameTimer m_gameTime;
		IRenderer m_renderer;

		ResourceManager m_resourceManager;
		SceneManager	m_sceneManager;
	public:
		AnarianGameEngine();
		virtual ~AnarianGameEngine();

		virtual void OnStart();
		virtual void OnEnd();

		virtual void Update();
		virtual void PreRender();
		virtual void Render();
		virtual void PostRender();
	};

}