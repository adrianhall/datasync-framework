using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Datasync.Client.Test.Attributes;

[ExcludeFromCodeCoverage]
public class RemoteTableEntityAttribute_Tests
{
    [Fact]
    public void Ctor_Default_SetsPath()
    {
        var sut = new RemoteTableEntityAttribute();
        sut.Path.Should().Be(string.Empty);
    }

    [Fact]
    public void Ctor_Path_SetsPath()
    {
        var sut = new RemoteTableEntityAttribute("foo");
        sut.Path.Should().Be("foo");
    }
}
