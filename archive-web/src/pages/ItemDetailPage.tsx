import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { items, wishlist } from '../api/client'
import type { FashionItemResponse, PriceHistoryResponse, WishlistEntryResponse } from '../types'
import { useAuth } from '../context/AuthContext'

function fmt(price: number, currency = 'BRL') {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).format(price)
}

export default function ItemDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const qc = useQueryClient()

  const { data: item, isLoading } = useQuery<FashionItemResponse>({
    queryKey: ['item', id],
    queryFn: () => items.getById(id!) as Promise<FashionItemResponse>,
    enabled: !!id,
  })

  const { data: history } = useQuery<PriceHistoryResponse[]>({
    queryKey: ['price-history', id],
    queryFn: () => items.getPriceHistory(id!) as Promise<PriceHistoryResponse[]>,
    enabled: !!id,
  })

  const { data: wishlistData } = useQuery<WishlistEntryResponse[]>({
    queryKey: ['wishlist'],
    queryFn: () => wishlist.getAll() as Promise<WishlistEntryResponse[]>,
    enabled: !!user,
  })

  const wishlistEntry = wishlistData?.find((w) => w.fashionItemId === id)

  const addMutation = useMutation({
    mutationFn: () => wishlist.add(id!),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['wishlist'] }),
  })

  const removeMutation = useMutation({
    mutationFn: () => wishlist.remove(wishlistEntry!.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['wishlist'] }),
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[calc(100vh-4rem)]">
        <span className="font-display text-2xl tracking-widest text-muted animate-pulse">
          CARREGANDO...
        </span>
      </div>
    )
  }

  if (!item) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[calc(100vh-4rem)]">
        <span className="font-display text-4xl tracking-widest text-muted mb-6">
          ITEM NÃO ENCONTRADO
        </span>
        <button
          onClick={() => navigate(-1)}
          className="text-xs text-muted hover:text-cream tracking-widest uppercase transition-colors"
        >
          ← Voltar
        </button>
      </div>
    )
  }

  const currency = item.currency || 'BRL'
  const prices = history?.map((h) => h.price) ?? []
  const minPrice = prices.length ? Math.min(...prices) : item.currentPrice
  const maxPrice = prices.length ? Math.max(...prices) : item.currentPrice
  const priceChange =
    history && history.length >= 2
      ? item.currentPrice - history[0].price
      : null

  return (
    <main className="max-w-6xl mx-auto px-6 py-12">
      <button
        onClick={() => navigate(-1)}
        className="text-xs tracking-widest text-muted uppercase hover:text-cream transition-colors mb-10 flex items-center gap-2"
      >
        ← Voltar
      </button>

      <div className="grid md:grid-cols-2 gap-12 lg:gap-20">
        {/* Image */}
        <div className="aspect-[3/4] bg-surface border border-border overflow-hidden">
          {item.imageUrl ? (
            <img src={item.imageUrl} alt={item.name} className="w-full h-full object-cover" />
          ) : (
            <div className="w-full h-full flex items-center justify-center">
              <span className="font-display text-7xl tracking-widest text-cream/10">ARCHIVÉ</span>
            </div>
          )}
        </div>

        {/* Info */}
        <div className="flex flex-col">
          <p className="text-xs tracking-widest text-muted uppercase mb-3">{item.brand}</p>
          <h1 className="font-display text-4xl lg:text-5xl tracking-wide text-cream leading-tight mb-2">
            {item.name.toUpperCase()}
          </h1>
          {item.category && (
            <p className="text-xs tracking-widest text-muted uppercase mb-8">{item.category}</p>
          )}

          <div className="mb-8">
            <p className="font-display text-5xl tracking-wider text-cream">
              {fmt(item.currentPrice, currency)}
            </p>
            <p className="text-xs text-muted mt-2 tracking-wider">
              Atualizado em{' '}
              {new Date(item.updatedAt).toLocaleDateString('pt-BR', {
                day: '2-digit',
                month: 'long',
                year: 'numeric',
              })}
            </p>
            {priceChange !== null && (
              <p
                className={`text-xs mt-1 tracking-wider font-medium ${
                  priceChange < 0 ? 'text-green-400' : priceChange > 0 ? 'text-red-400' : 'text-muted'
                }`}
              >
                {priceChange < 0 ? '↓' : priceChange > 0 ? '↑' : '='}{' '}
                {fmt(Math.abs(priceChange), currency)} em relação ao primeiro registro
              </p>
            )}
          </div>

          {/* Min / Max */}
          {prices.length > 1 && (
            <div className="flex gap-10 pb-8 mb-8 border-b border-border">
              <div>
                <p className="text-xs text-muted tracking-widest uppercase mb-1">Mínimo</p>
                <p className="font-display text-xl tracking-wider text-cream">
                  {fmt(minPrice, currency)}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted tracking-widest uppercase mb-1">Máximo</p>
                <p className="font-display text-xl tracking-wider text-cream">
                  {fmt(maxPrice, currency)}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted tracking-widest uppercase mb-1">Registros</p>
                <p className="font-display text-xl tracking-wider text-cream">{prices.length}</p>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3">
            {item.productUrl && (
              <a
                href={item.productUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex-1 bg-cream text-bg font-display tracking-widest text-center py-3 text-sm hover:bg-cream/90 transition-colors"
              >
                VER PRODUTO
              </a>
            )}
            {user && (
              <button
                onClick={() =>
                  wishlistEntry ? removeMutation.mutate() : addMutation.mutate()
                }
                disabled={addMutation.isPending || removeMutation.isPending}
                className={`px-6 py-3 font-display tracking-widest text-sm transition-colors border disabled:opacity-40 ${
                  wishlistEntry
                    ? 'bg-cream text-bg border-cream hover:bg-cream/90'
                    : 'border-border text-muted hover:border-cream hover:text-cream'
                }`}
              >
                {wishlistEntry ? '♥ SALVO' : '♡ SALVAR'}
              </button>
            )}
          </div>

          {/* Price History */}
          {history && history.length > 0 && (
            <div className="mt-12">
              <h2 className="font-display text-2xl tracking-widest text-cream mb-5">
                HISTÓRICO DE PREÇOS
              </h2>
              <div className="border-t border-border">
                {[...history]
                  .sort(
                    (a, b) =>
                      new Date(b.recordedAt).getTime() - new Date(a.recordedAt).getTime(),
                  )
                  .slice(0, 12)
                  .map((entry) => (
                    <div
                      key={entry.id}
                      className="flex items-center justify-between py-3 border-b border-border"
                    >
                      <span className="text-xs text-muted tracking-wider">
                        {new Date(entry.recordedAt).toLocaleDateString('pt-BR', {
                          day: '2-digit',
                          month: 'short',
                          year: 'numeric',
                        })}
                      </span>
                      <span className="text-xs text-muted uppercase tracking-widest">
                        {entry.source ?? '—'}
                      </span>
                      <span className="font-display text-base tracking-wider text-cream">
                        {fmt(entry.price, entry.currency)}
                      </span>
                    </div>
                  ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </main>
  )
}
