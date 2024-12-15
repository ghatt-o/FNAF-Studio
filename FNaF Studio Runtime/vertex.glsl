#version 330

layout(location = 0) in vec3 position;  // Vertex position
layout(location = 1) in vec2 texcoord;  // Texture coordinate

out vec2 fragTexCoord;                 // Passed to fragment shader

void main()
{
    gl_Position = vec4(position, 1.0); // Standard transformation
    fragTexCoord = texcoord;           // Pass texture coordinate to fragment shader
}
