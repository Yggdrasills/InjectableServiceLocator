Service Locator for Unity
===
Utility that adds code generation to the standard service locator. This repository offers to get acquainted with the basics of working with code generation (Mono.Cecil).
Allows you to automatically add services to the service locator using only an attribute to shorten the code.

Getting started
---
- [ ] Before patch disassembly

![Before patch disassembly](https://user-images.githubusercontent.com/48940160/189304711-480fae0d-b91e-4eb4-b206-256674a8b70c.png)

- [ ] After patch disassembly

![After patch disassembly](https://user-images.githubusercontent.com/48940160/189304793-62493d06-26a9-4437-84cf-8b4cdc77f10c.png)

Usage
---
1. Add `[Service(typeof(T))]` attribute for class that should be injected;
2. Add `[Inject]` attribute for field in the other classes where service should be injected.

```
[Service(typeof(DemoService))]
public class DemoService : MonoBehaviour
{
  public void Display()
  {
      Debug.Log("Demo service");
  }
}
```

```
public class DemoClass : MonoBehaviour
{
    [Inject]
    private DemoService _service;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _service.Display();
        }
    }
}
```

3. And don't forget to add ServiceLocator component to the scene

Notes
---
1. :small_red_triangle_down: This repository is for educational purposes only and is not recommended for commercial use, because contains a lot of uncovered injection cases.
