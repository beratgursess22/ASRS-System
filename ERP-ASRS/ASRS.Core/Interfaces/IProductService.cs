using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductListDto>> GetAllProductsAsync(string? search); // ürünleri listelemek için kullanılan servis arayüzü, arama özelliği de içerir
    Task<ProductListDto?> GetProductByIdAsync(int id); // belirli bir ürünü ID'sine göre getirmek için kullanılan servis arayüzü, ürün bulunamazsa null döner
    Task<bool> CreateProductAsync(CreateProductDto dto); // yeni bir ürün oluşturmak için kullanılan servis arayüzü, işlem başarılıysa true döner
    Task<bool> UpdateProductAsync(int id, CreateProductDto dto); // mevcut bir ürünü güncellemek için kullanılan servis arayüzü, ürün bulunamazsa veya işlem başarısız olursa false döner
    Task<bool> DeleteProductAsync(int id); // bir ürünü silmek için kullanılan servis arayüzü, ürün bulunamazsa veya işlem başarısız olursa false döner
}