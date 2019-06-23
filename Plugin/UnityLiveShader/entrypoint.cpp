#include <cstdint>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <memory>
#include <string>
#include <wrl/client.h>
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"

using namespace Microsoft::WRL;

IUnityGraphicsD3D11* unity_graphics_d3d11;

class renderer
{
public:
  void set_device(ID3D11Device* device)
  {
    d3d11_device = device;
  }

  void draw(void* data)
  {
    if (!prepared) return;

    ComPtr<ID3D11DeviceContext> device_context;
    d3d11_device->GetImmediateContext(&device_context);

    device_context->OMSetDepthStencilState(depth_stencil_state.Get(), 0);
    device_context->RSSetState(rasterizer_state.Get());
    device_context->OMSetBlendState(blend_state.Get(), nullptr, 0xFFFFFFFF);

    for (int i = 0; i < 16; i++)
    {
      constants.matrix[i] = reinterpret_cast<float*>(data)[i];
    }

    device_context->UpdateSubresource(constant_buffer.Get(), 0, nullptr, &constants, 64 + 16, 0);

    ID3D11Buffer* constant_buffers[] =
    {
      constant_buffer.Get(),
    };

    device_context->VSSetConstantBuffers(1, 1, constant_buffers);
    device_context->PSSetConstantBuffers(1, 1, constant_buffers);

    device_context->VSSetShader(vertex_shader.Get(), nullptr, 0);
    device_context->PSSetShader(pixel_shader.Get(), nullptr, 0);

    device_context->IASetInputLayout(input_layout.Get());
    device_context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    UINT stride = vertex_stride;
    UINT offset = 0;
    ID3D11Buffer* vertex_buffers[1] = { vertex_buffer };
    device_context->IASetVertexBuffers(0, 1, vertex_buffers, &stride, &offset);
    device_context->IASetIndexBuffer(index_buffer, (is_index_buffer_32bit_width) ? DXGI_FORMAT_R32_UINT : DXGI_FORMAT_R16_UINT, 0);

    device_context->DrawIndexed(index_count, 0, 0);
  }

  bool set_shader_code(const char* code)
  {
    shader_code = std::string(code);
    prepared = create_resources();
    return prepared;
  }

  void set_time(float time)
  {
    this->constants.time = time;
  }

  void set_vertex_buffer(ID3D11Buffer* buffer, size_t vertex_count)
  {
    this->vertex_count = vertex_count;
    D3D11_BUFFER_DESC desc;
    buffer->GetDesc(&desc);
    vertex_stride = desc.ByteWidth / vertex_count;
    vertex_buffer = buffer;
  }

  void set_index_buffer(ID3D11Buffer* buffer, size_t index_count, bool is_32bit_width)
  {
    this->index_count = index_count;
    is_index_buffer_32bit_width = is_32bit_width;
    index_buffer = buffer;
  }

private:
  ID3D11Device* d3d11_device;
  ComPtr<ID3D11Buffer> constant_buffer;
  ComPtr<ID3D11VertexShader> vertex_shader;
  ComPtr<ID3D11PixelShader> pixel_shader;
  ComPtr<ID3D11InputLayout> input_layout;
  ComPtr<ID3D11RasterizerState> rasterizer_state;
  ComPtr<ID3D11DepthStencilState> depth_stencil_state;
  ComPtr<ID3D11BlendState> blend_state;
  ID3D11Buffer* vertex_buffer;
  ID3D11Buffer* index_buffer;

  size_t index_count;
  size_t vertex_count;
  size_t vertex_stride;
  bool is_index_buffer_32bit_width;
  bool prepared = false;
  std::string shader_code;

  struct constants
  {
    float matrix[16];
    float time;
    float padding[3];
  };

  constants constants;

  bool create_resources()
  {
    ComPtr<ID3DBlob> vertex_shader_bytecode;
    ComPtr<ID3DBlob> error_message;
    D3DCompile(
      shader_code.c_str(),
      shader_code.length(),
      "simple_vertex_shader",
      nullptr,
      nullptr,
      "VS",
      "vs_5_0",
      0,
      0,
      &vertex_shader_bytecode,
      &error_message
    );

    if (error_message != nullptr)
    {
      OutputDebugString(L"Failed to compile vertex shader.");
      return false;
    }

    ComPtr<ID3DBlob> pixel_shader_bytecode;
    D3DCompile(
      shader_code.c_str(),
      shader_code.length(),
      "simple_pixel_shader",
      nullptr,
      nullptr,
      "PS",
      "ps_5_0",
      0,
      0,
      &pixel_shader_bytecode,
      &error_message
    );

    if (error_message != nullptr)
    {
      OutputDebugString(L"Failed to compile pixel shader.");
      return false;
    }

    D3D11_BUFFER_DESC buffer_desc;
    ZeroMemory(&buffer_desc, sizeof(buffer_desc));
    buffer_desc.ByteWidth = 64 + 16;
    buffer_desc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    buffer_desc.CPUAccessFlags = 0;
    d3d11_device->CreateBuffer(&buffer_desc, nullptr, &constant_buffer);

    auto hr = d3d11_device->CreateVertexShader(
      vertex_shader_bytecode->GetBufferPointer(),
      vertex_shader_bytecode->GetBufferSize(),
      nullptr,
      &vertex_shader
    );

    if (FAILED(hr))
    {
      OutputDebugString(L"Failed to create vertex shader.");
      return false;
    }

    hr = d3d11_device->CreatePixelShader(
      pixel_shader_bytecode->GetBufferPointer(),
      pixel_shader_bytecode->GetBufferSize(),
      nullptr,
      &pixel_shader
    );

    if (FAILED(hr))
    {
      OutputDebugString(L"Failed to create pixel shader.");
      return false;
    }

    D3D11_INPUT_ELEMENT_DESC input_element_desc[] =
    {
      { "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
      { "COLOR", 0, DXGI_FORMAT_R8G8B8A8_UNORM, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
    };
    hr = d3d11_device->CreateInputLayout(
      input_element_desc,
      2,
      vertex_shader_bytecode->GetBufferPointer(),
      vertex_shader_bytecode->GetBufferSize(),
      &input_layout
    );

    if (FAILED(hr))
    {
      OutputDebugString(L"Failed to create input layout.");
      return false;
    }

    D3D11_RASTERIZER_DESC rasterizer_desc;
    ZeroMemory(&rasterizer_desc, sizeof(rasterizer_desc));
    rasterizer_desc.FillMode = D3D11_FILL_SOLID;
    rasterizer_desc.CullMode = D3D11_CULL_NONE;
    rasterizer_desc.DepthClipEnable = TRUE;
    d3d11_device->CreateRasterizerState(&rasterizer_desc, &rasterizer_state);

    D3D11_DEPTH_STENCIL_DESC depth_stencil_desc;
    ZeroMemory(&depth_stencil_desc, sizeof(depth_stencil_desc));
    depth_stencil_desc.DepthEnable = TRUE;
    depth_stencil_desc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ZERO;
    depth_stencil_desc.DepthFunc = D3D11_COMPARISON_GREATER_EQUAL;
    d3d11_device->CreateDepthStencilState(&depth_stencil_desc, &depth_stencil_state);

    D3D11_BLEND_DESC blend_desc;
    ZeroMemory(&blend_desc, sizeof(blend_desc));
    blend_desc.RenderTarget[0].BlendEnable = FALSE;
    blend_desc.RenderTarget[0].RenderTargetWriteMask = 0xF;
    d3d11_device->CreateBlendState(&blend_desc, &blend_state);

    return true;
  }
};

std::unique_ptr<renderer> g_renderer;

static void UNITY_INTERFACE_API GraphicsDeviceEventCallback(UnityGfxDeviceEventType eventType)
{
  switch (eventType)
  {
  case UnityGfxDeviceEventType::kUnityGfxDeviceEventInitialize:
  case UnityGfxDeviceEventType::kUnityGfxDeviceEventAfterReset:
    g_renderer->set_device(unity_graphics_d3d11->GetDevice());
    break;
  case UnityGfxDeviceEventType::kUnityGfxDeviceEventBeforeReset:
  case UnityGfxDeviceEventType::kUnityGfxDeviceEventShutdown:
    g_renderer->set_device(nullptr);
    break;
  }
}

static void UNITY_INTERFACE_API Draw(int eventId, void* data)
{
  g_renderer->draw(data);
}

extern "C"
{
  void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
  {
    unity_graphics_d3d11 = unityInterfaces->Get<IUnityGraphicsD3D11>();
    g_renderer = std::make_unique<renderer>();
    GraphicsDeviceEventCallback(kUnityGfxDeviceEventInitialize);
  }

  UnityRenderingEventAndData UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetDrawCallback()
  {
    return Draw;
  }

  void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetTime(float time)
  {
    g_renderer->set_time(time);
  }

  // return 0 on success.
  int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetShaderCode(const char* code)
  {
    return g_renderer->set_shader_code(code) ? 0 : -1;
  }

  void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetVertexBuffer(ID3D11Buffer* buffer, int vertex_count)
  {
    g_renderer->set_vertex_buffer(buffer, static_cast<size_t>(vertex_count));
  }

  void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetIndexBuffer(ID3D11Buffer* buffer, unsigned int index_count, int index_format)
  {
    g_renderer->set_index_buffer(buffer, index_count, index_format == 1);
  }
}
