using AtMycelia.Hyphlow.EditorExt;

namespace VScriptingTests.VariableOperations
{
    class Animal { }
    class Mammal : Animal { }
    class Dog : Mammal { }
    class Reptile : Animal { }

    [RowVisualHandler("Testing",
        typeof(Animal),
        "Animal",
        "_EditorResources/Uxml/VarRows/VariableRowTemplate")]
    class AnimalHandler : RowVisualHandler<Animal> { }

    [RowVisualHandler("Testing",
        typeof(Mammal),
        "Mammal",
        "_EditorResources/Uxml/VarRows/VariableRowTemplate")]
    class MammalHandler : RowVisualHandler<Mammal> { }

    [RowVisualHandler("Testing",
        typeof(Dog),
        "Dog",
        "_EditorResources/Uxml/VarRows/VariableRowTemplate")]
    class DogHandler : RowVisualHandler<Dog> { }

    // We already have a default handler in the actual editor namespace, so no need to define it here


}