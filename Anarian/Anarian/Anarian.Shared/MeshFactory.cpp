#include "pch.h"

#if Anarian_DirectX_Mode
#include "DirectXMesh.h"
using namespace DirectX;
#endif

#include "PNTVertex.h"

#include "MeshFactory.h"
using namespace Anarian;


MeshFactory* MeshFactory::m_instance;
MeshFactory* MeshFactory::Instance()
{
	if (m_instance == nullptr) m_instance = new MeshFactory();
	return m_instance;
}

MeshFactory::MeshFactory()
{
}

MeshFactory::~MeshFactory()
{
	//delete m_instance;
}

IMeshObject* MeshFactory::ConstructCube()
{
	IMeshObject* cubeMesh;

	// Construct the MeshObject
#if Anarian_DirectX_Mode
	cubeMesh = new DirectXMesh();
#endif

	// Create the Vertices
	std::vector<Anarian::Verticies::PNTVertex> cubeVertices = std::vector<Anarian::Verticies::PNTVertex>();
	cubeVertices.reserve(24);

	//						 Position							Normal							Texture Coordinates
	cubeVertices.push_back({ XMFLOAT3(-0.5f, -0.5f, 0.5f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(0.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, -0.5f, 0.5f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(0.0f, 1.0f) });
	cubeVertices.push_back({ XMFLOAT3(-0.5f, 0.5f, 0.5f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(1.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, 0.5f, 0.5f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(1.0f, 1.0f) });

	cubeVertices.push_back({ XMFLOAT3(-0.5f, -0.5f, -0.5f),		XMFLOAT3(0.0f, 0.0f, -1.0f),	XMFLOAT2(0.0f, 0.0f) });   // side 2
	cubeVertices.push_back({ XMFLOAT3(-0.5f, 0.5f, -0.5f),		XMFLOAT3(0.0f, 0.0f, -1.0f),	XMFLOAT2(0.0f, 1.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, -0.5f, -0.5f),		XMFLOAT3(0.0f, 0.0f, -1.0f),	XMFLOAT2(1.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, 0.5f, -0.5f),		XMFLOAT3(0.0f, 0.0f, -1.0f),	XMFLOAT2(1.0f, 1.0f) });

	cubeVertices.push_back({ XMFLOAT3(-0.5f, 0.5f, -0.5f),		XMFLOAT3(0.0f, 1.0f, 0.0f),		XMFLOAT2(0.0f, 0.0f) });   // side 3
	cubeVertices.push_back({ XMFLOAT3(-0.5f, 0.5f, 0.5f),		XMFLOAT3(0.0f, 1.0f, 0.0f),		XMFLOAT2(0.0f, 1.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, 0.5f, -0.5f),		XMFLOAT3(0.0f, 1.0f, 0.0f),		XMFLOAT2(1.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, 0.5f, 0.5f),		XMFLOAT3(0.0f, 1.0f, 0.0f),		XMFLOAT2(1.0f, 1.0f) });

	cubeVertices.push_back({ XMFLOAT3(-0.5f, -0.5f, -0.5f),		XMFLOAT3(0.0f, -1.0f, 0.0f),	XMFLOAT2(0.0f, 0.0f) });    // side 4
	cubeVertices.push_back({ XMFLOAT3(0.5f, -0.5f, -0.5f),		XMFLOAT3(0.0f, -1.0f, 0.0f),	XMFLOAT2(0.0f, 1.0f) });
	cubeVertices.push_back({ XMFLOAT3(-0.5f, -0.5f, 0.5f),		XMFLOAT3(0.0f, -1.0f, 0.0f),	XMFLOAT2(1.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, -0.5f, 0.5f),		XMFLOAT3(0.0f, -1.0f, 0.0f),	XMFLOAT2(1.0f, 1.0f) });

	cubeVertices.push_back({ XMFLOAT3(0.5f, -0.5f, -0.5f),		XMFLOAT3(1.0f, 0.0f, 0.0f),		XMFLOAT2(0.0f, 0.0f) });   // side 5
	cubeVertices.push_back({ XMFLOAT3(0.5f, 0.5f, -0.5f),		XMFLOAT3(1.0f, 0.0f, 0.0f),		XMFLOAT2(0.0f, 1.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, -0.5f, 0.5f),		XMFLOAT3(1.0f, 0.0f, 0.0f),		XMFLOAT2(1.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(0.5f, 0.5f, 0.5f),		XMFLOAT3(1.0f, 0.0f, 0.0f),		XMFLOAT2(1.0f, 1.0f) });

	cubeVertices.push_back({ XMFLOAT3(-0.5f, -0.5f, -0.5f),		XMFLOAT3(-1.0f, 0.0f, 0.0f),	XMFLOAT2(0.0f, 0.0f) });   // side 6
	cubeVertices.push_back({ XMFLOAT3(-0.5f, -0.5f, 0.5f),		XMFLOAT3(-1.0f, 0.0f, 0.0f),	XMFLOAT2(0.0f, 1.0f) });
	cubeVertices.push_back({ XMFLOAT3(-0.5f, 0.5f, -0.5f),		XMFLOAT3(-1.0f, 0.0f, 0.0f),	XMFLOAT2(1.0f, 0.0f) });
	cubeVertices.push_back({ XMFLOAT3(-0.5f, 0.5f, 0.5f),		XMFLOAT3(-1.0f, 0.0f, 0.0f),	XMFLOAT2(1.0f, 1.0f) });

	// Create the Indices
	std::vector<short> cubeIndices = std::vector<short>();
	cubeIndices.reserve(36);
	cubeIndices.push_back(0);	cubeIndices.push_back(1);	cubeIndices.push_back(2); // side 1
	cubeIndices.push_back(2);	cubeIndices.push_back(1);	cubeIndices.push_back(3);
	cubeIndices.push_back(4);	cubeIndices.push_back(5);	cubeIndices.push_back(6); // side 2
	cubeIndices.push_back(6);	cubeIndices.push_back(5);	cubeIndices.push_back(7);
	cubeIndices.push_back(8);	cubeIndices.push_back(9);	cubeIndices.push_back(10); // side 3
	cubeIndices.push_back(10);	cubeIndices.push_back(9);	cubeIndices.push_back(11);
	cubeIndices.push_back(12);	cubeIndices.push_back(13);	cubeIndices.push_back(14); // side 4
	cubeIndices.push_back(14);	cubeIndices.push_back(13);	cubeIndices.push_back(15);
	cubeIndices.push_back(16);	cubeIndices.push_back(17);	cubeIndices.push_back(18); // side 5
	cubeIndices.push_back(18);	cubeIndices.push_back(17);	cubeIndices.push_back(19);
	cubeIndices.push_back(20);	cubeIndices.push_back(21);	cubeIndices.push_back(22); // side 6
	cubeIndices.push_back(22);	cubeIndices.push_back(21);	cubeIndices.push_back(23);

	// Apply cross-platform variables
	cubeMesh->m_vertices = cubeVertices;
	cubeMesh->m_indices = cubeIndices;

	cubeMesh->m_vertexCount = cubeVertices.size();
	cubeMesh->m_indexCount = cubeIndices.size();

	return cubeMesh;
}

IMeshObject* MeshFactory::ConstructFace()
{
	IMeshObject* faceMesh;

	// Construct the MeshObject
#if Anarian_DirectX_Mode
	faceMesh = new DirectXMesh();
#endif

	// Create the Vertices
	std::vector<Anarian::Verticies::PNTVertex> faceVertices = std::vector<Anarian::Verticies::PNTVertex>();
	faceVertices.reserve(4);

	//						 Position							Normal							Texture Coordinates
	faceVertices.push_back({ XMFLOAT3(0.0f, 0.0f, 0.0f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(1.0f, 1.0f) });
	faceVertices.push_back({ XMFLOAT3(1.0f, 0.0f, 0.0f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(0.0f, 1.0f) });
	faceVertices.push_back({ XMFLOAT3(1.0f, 1.0f, 0.0f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(0.0f, 0.0f) });
	faceVertices.push_back({ XMFLOAT3(0.0f, 1.0f, 0.0f),		XMFLOAT3(0.0f, 0.0f, 1.0f),		XMFLOAT2(1.0f, 0.0f) });

	// Create the Indices
	std::vector<short> faceIndices = std::vector<short>();
	faceIndices.reserve(12);
	faceIndices.push_back(0);	faceIndices.push_back(1);	faceIndices.push_back(2);
	faceIndices.push_back(0);	faceIndices.push_back(2);	faceIndices.push_back(3);
	faceIndices.push_back(0);	faceIndices.push_back(2);	faceIndices.push_back(1);
	faceIndices.push_back(0);	faceIndices.push_back(3);	faceIndices.push_back(2);

	// Apply cross-platform variables
	faceMesh->m_vertices = faceVertices;
	faceMesh->m_indices = faceIndices;

	faceMesh->m_vertexCount = faceVertices.size();	// m_vertexCount = 4;
	faceMesh->m_indexCount = faceIndices.size();	// m_indexCount = 12;

	return faceMesh;
}

IMeshObject* MeshFactory::ConstructCylinder(uint32 segments)
{
	IMeshObject* cylinderMesh;

	// Construct the MeshObject
#if Anarian_DirectX_Mode
	cylinderMesh = new DirectXMesh();
#endif

	uint32 numVertices = 6 * (segments + 1) + 1;
	uint32 numIndices = 3 * segments * 3 * 2;

	std::vector<Anarian::Verticies::PNTVertex> point(numVertices);
	std::vector<short> index(numIndices);

	uint32 p = 0;
	// Top center point (multiple points for texture coordinates).
	for (uint32 a = 0; a <= segments; a++)
	{
		point[p].position = XMFLOAT3(0.0f, 0.0f, 1.0f);
		point[p].normal = XMFLOAT3(0.0f, 0.0f, 1.0f);
		point[p].textureCoordinate = XMFLOAT2(static_cast<float>(a) / static_cast<float>(segments), 0.0f);
		p++;
	}
	// Top edge of cylinder: Normals point up for lighting of top surface.
	for (uint32 a = 0; a <= segments; a++)
	{
		float angle = static_cast<float>(a) / static_cast<float>(segments)* XM_2PI;
		point[p].position = XMFLOAT3(cos(angle), sin(angle), 1.0f);
		point[p].normal = XMFLOAT3(0.0f, 0.0f, 1.0f);
		point[p].textureCoordinate = XMFLOAT2(static_cast<float>(a) / static_cast<float>(segments), 0.0f);
		p++;
	}
	// Top edge of cylinder: Normals point out for lighting of the side surface.
	for (uint32 a = 0; a <= segments; a++)
	{
		float angle = static_cast<float>(a) / static_cast<float>(segments)* XM_2PI;
		point[p].position = XMFLOAT3(cos(angle), sin(angle), 1.0f);
		point[p].normal = XMFLOAT3(cos(angle), sin(angle), 0.0f);
		point[p].textureCoordinate = XMFLOAT2(static_cast<float>(a) / static_cast<float>(segments), 0.0f);
		p++;
	}
	// Bottom edge of cylinder: Normals point out for lighting of the side surface.
	for (uint32 a = 0; a <= segments; a++)
	{
		float angle = static_cast<float>(a) / static_cast<float>(segments)* XM_2PI;
		point[p].position = XMFLOAT3(cos(angle), sin(angle), 0.0f);
		point[p].normal = XMFLOAT3(cos(angle), sin(angle), 0.0f);
		point[p].textureCoordinate = XMFLOAT2(static_cast<float>(a) / static_cast<float>(segments), 1.0f);
		p++;
	}
	// Bottom edge of cylinder: Normals point down for lighting of the bottom surface.
	for (uint32 a = 0; a <= segments; a++)
	{
		float angle = static_cast<float>(a) / static_cast<float>(segments)* XM_2PI;
		point[p].position = XMFLOAT3(cos(angle), sin(angle), 0.0f);
		point[p].normal = XMFLOAT3(0.0f, 0.0f, -1.0f);
		point[p].textureCoordinate = XMFLOAT2(static_cast<float>(a) / static_cast<float>(segments), 1.0f);
		p++;
	}
	// Bottom center of cylinder: Normals point down for lighting on the bottom surface.
	for (uint32 a = 0; a <= segments; a++)
	{
		point[p].position = XMFLOAT3(0.0f, 0.0f, 0.0f);
		point[p].normal = XMFLOAT3(0.0f, 0.0f, -1.0f);
		point[p].textureCoordinate = XMFLOAT2(static_cast<float>(a) / static_cast<float>(segments), 1.0f);
		p++;
	}
	//cylinderMesh->m_vertexCount = p;

	p = 0;
	for (uint16 a = 0; a < 6; a += 2)
	{
		uint16 p1 = a*(segments + 1);
		uint16 p2 = (a + 1)*(segments + 1);
		for (uint16 b = 0; b < segments; b++)
		{
			if (a < 4)
			{
				index[p] = b + p1;
				index[p + 1] = b + p2;
				index[p + 2] = b + p2 + 1;
				p = p + 3;
			}
			if (a > 0)
			{
				index[p] = b + p1;
				index[p + 1] = b + p2 + 1;
				index[p + 2] = b + p1 + 1;
				p = p + 3;
			}
		}
	}
	//cylinderMesh->m_indexCount = p;

	// Apply cross-platform variables
	cylinderMesh->m_vertices = point;
	cylinderMesh->m_indices = index;

	cylinderMesh->m_vertexCount = point.size();
	cylinderMesh->m_indexCount = index.size();

	return cylinderMesh;
}

IMeshObject* MeshFactory::ConstructSphere(uint32 segments)
{
	IMeshObject* sphereMesh;

	// Construct the MeshObject
#if Anarian_DirectX_Mode
	sphereMesh = new DirectXMesh();
#endif

	uint32 slices = segments / 2;
	uint32 numVertices = (slices + 1) * (segments + 1) + 1;
	uint32 numIndices = slices * segments * 3 * 2;

	std::vector<Anarian::Verticies::PNTVertex> point(numVertices);
	std::vector<short> index(numIndices);

	// To make the texture look right on the top and bottom of the sphere
	// each slice will have 'segments + 1' vertices.  The top and bottom
	// vertices will all be coincident, but have different U texture cooordinates.
	uint32 p = 0;
	for (uint32 a = 0; a <= slices; a++)
	{
		float angle1 = static_cast<float>(a) / static_cast<float>(slices)* XM_PI;
		float z = cos(angle1);
		float r = sin(angle1);
		for (uint32 b = 0; b <= segments; b++)
		{
			float angle2 = static_cast<float>(b) / static_cast<float>(segments)* XM_2PI;
			point[p].position = XMFLOAT3(r * cos(angle2), r * sin(angle2), z);
			point[p].normal = point[p].position;
			point[p].textureCoordinate = XMFLOAT2((1.0f - z) / 2.0f, static_cast<float>(b) / static_cast<float>(segments));
			p++;
		}
	}
	//sphereMesh->m_vertexCount = p;

	p = 0;
	for (uint16 a = 0; a < slices; a++)
	{
		uint16 p1 = a * (segments + 1);
		uint16 p2 = (a + 1) * (segments + 1);

		// Generate two triangles for each segment around the slice.
		for (uint16 b = 0; b < segments; b++)
		{
			if (a < (slices - 1))
			{
				// For all but the bottom slice add the triangle with one
				// vertex in the a slice and two vertices in the a + 1 slice.
				// Skip it for the bottom slice since the triangle would be
				// degenerate as all the vertices in the bottom slice are coincident.
				index[p] = b + p1;
				index[p + 1] = b + p2;
				index[p + 2] = b + p2 + 1;
				p = p + 3;
			}
			if (a > 0)
			{
				// For all but the top slice add the triangle with two
				// vertices in the a slice and one vertex in the a + 1 slice.
				// Skip it for the top slice since the triangle would be
				// degenerate as all the vertices in the top slice are coincident.
				index[p] = b + p1;
				index[p + 1] = b + p2 + 1;
				index[p + 2] = b + p1 + 1;
				p = p + 3;
			}
		}
	}
	//sphereMesh->m_indexCount = p;

	// Apply cross-platform variables
	sphereMesh->m_vertices = point;
	sphereMesh->m_indices = index;

	sphereMesh->m_vertexCount = point.size();
	sphereMesh->m_indexCount = index.size();

	return sphereMesh;
}