// Unity 6 / paket API stublari: faqat kompilyatsiya tekshiruvi uchun
namespace Unity.InferenceEngine {
  public class ModelAsset : UnityEngine.Object {}
  public class Model {}
  public enum BackendType { GPUCompute, CPU, GPUPixel }
  public static class ModelLoader { public static Model Load(ModelAsset a) => null; }
  public struct TensorShape { public TensorShape(params int[] d) {} }
  public abstract class Tensor : System.IDisposable { public void Dispose() {} }
  public class Tensor<T> : Tensor { public Tensor(TensorShape s, T[] data = null) {} public T[] DownloadToArray() => null; }
  public class Worker : System.IDisposable {
    public Worker(Model m, BackendType b) {}
    public void Schedule(params Tensor[] t) {}
    public Tensor PeekOutput(string name) => null;
    public Tensor PeekOutput() => null;
    public void SetInput(string n, Tensor t) {}
    public void Dispose() {}
  }
}
