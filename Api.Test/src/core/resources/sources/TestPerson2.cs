#pragma warning disable CA1050 // Declare types in namespaces
public class TestPerson2
#pragma warning restore CA1050 // Declare types in namespaces
{
    public TestPerson2(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; }

    public string LastName { get; }

    public string FullName => FirstName + " " + LastName;

    public string FullName2() => FirstName + " " + LastName;

    public string FullName3() => FirstName + " " + LastName;
}
