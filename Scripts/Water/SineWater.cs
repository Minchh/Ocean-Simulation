using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class SineWater : MeshInstance3D
{
    #region Mesh Settings
    [ExportGroup("Mesh Settings")]

    [Export]
    public int QuadResolution { get; set; } = 100;

    [Export]
    public int PlaneLength { get; set; } = 100;

    #endregion
    
    #region Water Visual Settings
    [ExportGroup("Water Visual Settings")]
    [Export]
    public Color ShallowWaterColor { get; set; } = new Color(0.1f, 0.4f, 0.6f);
    
    [Export]
    public Color DeepWaterColor { get; set; } = new Color(0.0f, 0.05f, 0.2f);

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Metallic { get; set; }

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Roughness { get; set; } = 0.4f;

    [Export(PropertyHint.Range, "0.0,2.0")]
    public float SpecularStrength { get; set; } = 1.0f; 
    #endregion

    #region Procedural Settings
    [ExportGroup("Procedural Settings")]
    
    [Export(PropertyHint.Range, "1,64,1")]
    public int WaveCount { set; get; } = 4;

    [Export]
    public float MedianWavelength { set; get; } = 1.0f;

    [Export]
    public float MedianDirectionalDegree { get; set; }

    [Export]
    public float DirectionalDegreeRange { get; set; } = 30.0f;

    [Export]
    public float MedianAmplitude { get; set; } = 1.0f;
    #endregion

    public struct Wave
    {
        public float Amplitude;
        public Vector2 Direction;
        public float WaveNumber;
        public float AngularFrequency;

        public Wave(float amplitude, float directionalDegree, float wavelength)
        {
            Amplitude = amplitude;

            var angle = Mathf.DegToRad(directionalDegree);
            Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized();

            WaveNumber = 2.0f * Mathf.Pi / wavelength;
            
            // Dispersion relation for water (Tessendorf 2001)
            AngularFrequency = Mathf.Sqrt(9.8f * WaveNumber);
        }
    }

    private Shader m_SineWaterShader;
    private List<Wave> m_Waves = new List<Wave>(64);

    public override void _Ready()
    {
        // GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;
        CreateMesh();
        CreateMaterial();
        GenerateWaves();
        UploadShaderParameters();
    }

    private void GenerateWaves()
    {
        // Ranges
        float waveLengthMin = MedianWavelength / 2.0f;
        float waveLengthMax = MedianWavelength * 2.0f;

        float directionalDegreeMin = MedianDirectionalDegree - DirectionalDegreeRange;
        float directionalDegreeMax = MedianDirectionalDegree + DirectionalDegreeRange;

        // Ratio for calculating wave's amplitude
        float r = MedianAmplitude / MedianWavelength;

        var rng = new RandomNumberGenerator();
        for (int i = 0; i < WaveCount; i++)
        {
            float waveLength = rng.RandfRange(waveLengthMin, waveLengthMax);
            float directionalDegree = rng.RandfRange(directionalDegreeMin, directionalDegreeMax);
            float amplitude = waveLength * r;
            
            m_Waves.Add(new Wave(amplitude, directionalDegree, waveLength));
        }
    }
    
    private void CreateMesh()
    {
        var arrayMesh = new ArrayMesh();
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        float halfLength = PlaneLength * 0.5f;
        
        var vertices = new List<Vector3>((QuadResolution + 1) * (QuadResolution + 1));
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        float step = (float)PlaneLength / QuadResolution;
        for (int z = 0; z < QuadResolution + 1; z++)
        {
            for (int x = 0; x < QuadResolution + 1; x++)
            {
                vertices.Add(new Vector3(-halfLength + x * step, 0.0f, -halfLength + z * step));
                
                uvs.Add(new Vector2((float)x / QuadResolution, (float)z / QuadResolution));
                
                normals.Add(Vector3.Up);
            }
        }

        for (int z = 0; z < QuadResolution; z++)
        {
            for (int x = 0; x < QuadResolution; x++)
            {
                int index = z * (QuadResolution + 1) + x;
                
                indices.AddRange([
                    index, index + 1, index + (QuadResolution + 1) + 1,
                    index, index + (QuadResolution + 1) + 1, index + (QuadResolution + 1),
                ]);
            }
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        Mesh = arrayMesh;
    }

    private void CreateMaterial()
    {
        m_SineWaterShader ??= GD.Load<Shader>("res://Resources/Shaders/Water/SineWater.gdshader");

        var waterMaterial = new ShaderMaterial()
        {
            Shader = m_SineWaterShader,
        };
        
        Mesh.SurfaceSetMaterial(0, waterMaterial);
    }

    private void UploadShaderParameters()
    {
        var material = Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
        if (material == null) return;
        
        var amplitudes = new float[WaveCount];
        var directions = new Vector2[WaveCount];
        var waveNumbers = new float[WaveCount];
        var angularFrequencies = new float[WaveCount];

        for (int i = 0; i < WaveCount; i++)
        {
            amplitudes[i] = m_Waves[i].Amplitude;
            directions[i] = m_Waves[i].Direction;
            waveNumbers[i] = m_Waves[i].WaveNumber;
            angularFrequencies[i] = m_Waves[i].AngularFrequency;
        }
        
        material.SetShaderParameter("u_wave_count", WaveCount);
        material.SetShaderParameter("u_amplitudes", amplitudes);
        material.SetShaderParameter("u_directions", directions);
        material.SetShaderParameter("u_wave_numbers", waveNumbers);
        material.SetShaderParameter("u_angular_frequencies", angularFrequencies);
        material.SetShaderParameter("u_shallow_color", ShallowWaterColor);
        material.SetShaderParameter("u_deep_color", DeepWaterColor);
        material.SetShaderParameter("u_metallic", Metallic);
        material.SetShaderParameter("u_roughness", Roughness);
        material.SetShaderParameter("u_specular_strength", SpecularStrength);
        
    }
}
