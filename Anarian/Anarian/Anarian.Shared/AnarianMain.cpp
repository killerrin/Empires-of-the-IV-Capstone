﻿#include "pch.h"

#include "AnarianMain.h"
#include "Common\DirectXHelper.h"

//#include "BasicLoader.h"
#include "WICTextureLoader.h"

#include "GameObject.h"
#include "DirectXMaterial.h"
#include "DirectXMesh.h"

#include "RendererFactory.h"
#include "MeshFactory.h"
#include "MaterialFactory.h"

using namespace Anarian;
using namespace Windows::Foundation;
using namespace Windows::System::Threading;
using namespace Concurrency;

// Loads and initializes application assets when the application is loaded.
AnarianMain::AnarianMain(const std::shared_ptr<DX::DeviceResources>& deviceResources) :
	m_deviceResources(deviceResources), m_pointerLocationX(0.0f)
{
	// Register to be notified if the Device is lost or recreated
	m_deviceResources->RegisterDeviceNotify(this);

	// Initialize the Managers
	m_resourceManager = std::shared_ptr<ResourceManager> (new ResourceManager());
	m_sceneManager = std::shared_ptr<SceneManager> (new SceneManager());

	// Make the GameTimer
	m_gameTime = GameTimer();

	// Initialize the factories
	RendererFactory::Instance();
	MeshFactory::Instance();
	MaterialFactory::Instance();

	//---- Testing Code
	//-- Setup an empty scene
	m_sceneManager->SetCurrentScene(new IScene());

	// Create the Mesh
	IMeshObject* mesh = MeshFactory::Instance()->ConstructCube();
	((DirectXMesh*)mesh)->CreateBuffers(m_deviceResources->GetD3DDevice());
	m_resourceManager->AddMesh("cube", mesh);

	// Create Material
	IMaterial* material = MaterialFactory::Instance()->ConstructMaterial(
		Color(0.5f, 1.0f, 0.4f, 0.5f),
		Color(0.0f, 1.0, 0.5f, 0.5f),
		Color(0.5f, 0.5f, 0.5f, 0.5f),
		1.0f);
	m_resourceManager->AddMaterial("material", material);

	// Create the Game Object
	GameObject* gameObject = new GameObject();
	gameObject->SetActive(false);
	gameObject->SetMaterial(m_resourceManager->GetMaterial("material"));
	gameObject->SetMesh(m_resourceManager->GetMesh("cube"));

	GameObject* g2 = new GameObject();
	g2->SetMaterial(m_resourceManager->GetMaterial("material"));
	g2->SetMesh(m_resourceManager->GetMesh("cube"));
	g2->Position(DirectX::XMFLOAT3(0.5f, 0.5f, 0.0f));
	g2->Scale(DirectX::XMFLOAT3(0.5f, 0.5f, 0.5f));

	gameObject->AddChild(g2);
	m_sceneManager->GetCurrentScene()->GetSceneNode()->AddChild(gameObject);


	// TODO: Replace this with your app's content initialization.
	m_sceneRenderer = RendererFactory::Instance()->ConstructRenderer(m_sceneManager, m_resourceManager, Color::CornFlowerBlue());
	((Sample3DSceneRenderer*)m_sceneRenderer)->Initialize(m_deviceResources);

	m_fpsTextRenderer = std::unique_ptr<SampleFpsTextRenderer>(new SampleFpsTextRenderer(m_deviceResources));

	// TODO: Change the timer settings if you want something other than the default variable timestep mode.
	// e.g. for 60 FPS fixed timestep update logic, call:
	/*
	m_timer.SetFixedTimeStep(true);
	m_timer.SetTargetElapsedSeconds(1.0 / 60);
	*/
}

AnarianMain::~AnarianMain()
{
	// Deregister device notification
	m_deviceResources->RegisterDeviceNotify(nullptr);

	// Delete the Managers -disabled because they are now SharedPointers
	//delete m_resourceManager;
	//delete m_sceneManager;

	// Delete the Factories
	delete RendererFactory::Instance();
	delete MeshFactory::Instance();
	delete MaterialFactory::Instance();

	// Delete the Renderer
	delete m_sceneRenderer;
}

// Updates application state when the window size changes (e.g. device orientation change)
void AnarianMain::CreateWindowSizeDependentResources() 
{
	// TODO: Replace this with the size-dependent initialization of your app's content.
	((Sample3DSceneRenderer*)m_sceneRenderer)->CreateWindowSizeDependentResources();
}

void AnarianMain::StartRenderLoop()
{
	// If the animation render loop is already running then do not start another thread.
	if (m_renderLoopWorker != nullptr && m_renderLoopWorker->Status == AsyncStatus::Started)
	{
		return;
	}

	// Create a task that will be run on a background thread.
	auto workItemHandler = ref new WorkItemHandler([this](IAsyncAction ^ action)
	{
		// Calculate the updated frame and render once per vertical blanking interval.
		while (action->Status == AsyncStatus::Started)
		{
			critical_section::scoped_lock lock(m_criticalSection);
			Update();
			if (Render())
			{
				m_deviceResources->Present();
			}
		}
	});

	// Run task on a dedicated high priority background thread.
	m_renderLoopWorker = ThreadPool::RunAsync(workItemHandler, WorkItemPriority::High, WorkItemOptions::TimeSliced);
}

void AnarianMain::StopRenderLoop()
{
	m_renderLoopWorker->Cancel();
}

// Updates the application state once per frame.
void AnarianMain::Update() 
{
	// Update the GameTime
	m_gameTime.Update();

	ProcessInput();

	// Update scene objects.
	m_timer.Tick([&]()
	{
		// TODO: Replace this with your app's content update functions.
		((Sample3DSceneRenderer*)m_sceneRenderer)->Update(m_timer, &m_gameTime);
		m_fpsTextRenderer->Update(m_timer);
	});
}

// Process all input from the user before updating game state
void AnarianMain::ProcessInput()
{
	// TODO: Add per frame input handling here.
	((Sample3DSceneRenderer*)m_sceneRenderer)->TrackingUpdate(m_pointerLocationX, m_pointerLocationY);
}

// Renders the current frame according to the current application state.
// Returns true if the frame was rendered and is ready to be displayed.
bool AnarianMain::Render() 
{
	// Don't try to render anything before the first Update.
	if (m_timer.GetFrameCount() == 0)
	{
		return false;
	}

	// Render the scene objects.
	// TODO: Replace this with your app's content rendering functions.

	((Sample3DSceneRenderer*)m_sceneRenderer)->Render();
	m_fpsTextRenderer->Render();

	return true;
}

// Notifies renderers that device resources need to be released.
void AnarianMain::OnDeviceLost()
{
	//m_sceneRenderer->ReleaseDeviceDependentResources();
	m_fpsTextRenderer->ReleaseDeviceDependentResources();
}

// Notifies renderers that device resources may now be recreated.
void AnarianMain::OnDeviceRestored()
{
	//m_sceneRenderer->CreateDeviceDependentResources();
	m_fpsTextRenderer->CreateDeviceDependentResources();
	CreateWindowSizeDependentResources();
}