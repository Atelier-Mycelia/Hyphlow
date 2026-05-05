using AtMycelia.Hyphlow.Sys;
using AtMycelia.AmaniTween;
using NUnit.Framework;
using UnityEngine;
using UnityObj = UnityEngine.Object;

public class DefaultAdapterTests : MonoBehaviour
{
    protected DefaultTweenAdapter _adapter;

    protected const float Duration = 1f;
    protected const float Epsilon = 1e-3f;

    [SetUp]
    public virtual void SetUp()
    {
        _testGo = new GameObject("TweenTestGO");
        _adapter = DefaultTweener; 
    }

    private static DefaultTweenAdapter DefaultTweener => HyphlowRuntimeSysAssets.S.TweenAdapter;
    protected GameObject _testGo;

    [TearDown]
    public virtual void TearDown()
    {
        if (_testGo) UnityObj.DestroyImmediate(_testGo);
    }
}
